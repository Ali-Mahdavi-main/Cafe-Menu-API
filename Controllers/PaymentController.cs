using System.Security.Claims;
using CafeMenu.Api.Data;
using CafeMenu.Api.Dtos.Payment;
using CafeMenu.Api.Models;
using CafeMenu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ISubscriptionService _subscriptionService;

    public PaymentController(IPaymentService paymentService, AppDbContext context, IConfiguration config, ISubscriptionService subscriptionService)
    {
        _paymentService = paymentService;
        _context = context;
        _config = config;
        _subscriptionService = subscriptionService;
    }

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.DurationDays,
                p.Price
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestPayment([FromBody] PaymentRequestDto dto)
    {
        if (dto.PlanId <= 0)
            return BadRequest("PlanId is required.");

        var cafeIdClaim = User.FindFirstValue("CafeId");
        if (!int.TryParse(cafeIdClaim, out var cafeId))
            return Unauthorized();

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.IsActive);

        if (plan is null)
            return BadRequest("Selected plan is invalid.");

        var activeSubscriptionExists = await _context.CafeSubscriptions
            .AnyAsync(s => s.CafeId == cafeId && s.IsActive && s.EndDate > DateTime.UtcNow);

        if (activeSubscriptionExists)
            return Conflict("This cafe already has an active subscription.");

        var subscription = new CafeSubscription
        {
            CafeId = cafeId,
            PlanId = plan.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(plan.DurationDays),
            IsActive = false,
            WarningCount = 0
        };

        _context.CafeSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var payment = new Payment
        {
            CafeId = cafeId,
            SubscriptionId = subscription.Id,
            Amount = plan.Price,
            Currency = "IRR",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/payment/verify?subscriptionId={subscription.Id}&paymentId={payment.Id}";
        var description = $"Subscription {plan.Name} payment for cafe {cafeId}";

        var authority = await _paymentService.CreateRequestAsync((int)plan.Price, callbackUrl, description);
        if (string.IsNullOrWhiteSpace(authority))
        {
            payment.Status = "Pending";
            payment.Authority = "local-demo";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                authority = "local-demo",
                redirectUrl = $"{Request.Scheme}://{Request.Host}/api/payment/verify?subscriptionId={subscription.Id}&paymentId={payment.Id}&status=OK&authority=local-demo"
            });
        }

        payment.Authority = authority;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            authority,
            redirectUrl = $"https://sandbox.zarinpal.com/pg/StartPay/{authority}"
        });
    }

    [HttpGet("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment([FromQuery] string? authority, [FromQuery] string? status, [FromQuery] int subscriptionId, [FromQuery] int paymentId)
    {
        if (string.IsNullOrWhiteSpace(authority))
            return BadRequest("Authority is required.");

        if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(_config["Frontend:PaymentCancelUrl"] ?? "/dashboard?payment=cancelled");
        }

        var payment = await _context.Payments
            .Include(p => p.Subscription)
            .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.SubscriptionId == subscriptionId && p.Authority == authority);

        if (payment is null)
            return NotFound("Payment record was not found.");

        if (payment.Status == "Success")
            return Redirect(_config["Frontend:PaymentSuccessUrl"] ?? "/dashboard?payment=success");

        var verification = await _paymentService.VerifyRequestAsync(authority, (int)payment.Amount);
        if (!verification.IsSuccess)
        {
            payment.Status = "Failed";
            await _context.SaveChangesAsync();
            return Redirect(_config["Frontend:PaymentFailedUrl"] ?? "/dashboard?payment=failed");
        }

        payment.Status = "Success";
        payment.ReferenceId = verification.RefId?.ToString();
        payment.CompletedAt = DateTime.UtcNow;

        var subscriptionActivated = await _subscriptionService.ActivateSubscriptionAsync(payment.CafeId, payment.SubscriptionId, authority, verification.RefId ?? 0);
        if (!subscriptionActivated)
        {
            payment.Status = "Failed";
            await _context.SaveChangesAsync();
            return Redirect(_config["Frontend:PaymentFailedUrl"] ?? "/dashboard?payment=failed");
        }

        await _context.SaveChangesAsync();
        return Redirect(_config["Frontend:PaymentSuccessUrl"] ?? "/dashboard?payment=success");
    }
}