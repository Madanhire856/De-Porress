using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using SMS.Web.Services;
using System;
using System.Threading.Tasks;

namespace SMS.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IRbzRateService _rbz;

        public IndexModel(IRbzRateService rbz)
        {
            _rbz = rbz;
        }

        public string? SmokeTestOutput { get; set; }

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return;

            Console.WriteLine("========================================");
            Console.WriteLine("[SmokeTest] START — VICE VERSA");
            Console.WriteLine($"[SmokeTest] Base currency = ZWG (we flipped it)");
            Console.WriteLine($"[SmokeTest] Asking for USD rate for {DateTime.Today:yyyy-MM-dd}");
            Console.WriteLine("========================================");

            try
            {
                // ← CHANGED: ask for USD (foreign when ZWG is base)
                var result = await _rbz.GetRateAsync("USD", DateTime.Today);

                if (result == null)
                {
                    SmokeTestOutput = "SMOKE TEST: service returned NULL";
                    Console.WriteLine("[SmokeTest] RESULT: NULL");
                    return;
                }

                SmokeTestOutput =
                    $"Rate: {result.Rate}, " +
                    $"Requested: {result.RequestedDate:yyyy-MM-dd}, " +
                    $"Actual: {result.ActualRateDate:yyyy-MM-dd}, " +
                    $"Source: {result.Source}, " +
                    $"FetchedLive: {result.WasFetchedLive}, " +
                    $"IsStale: {result.IsStale}";

                Console.WriteLine("----------------------------------------");
                Console.WriteLine("[SmokeTest] RESULT: SUCCESS");
                Console.WriteLine($"[SmokeTest]   Rate          = {result.Rate}");
                Console.WriteLine($"[SmokeTest]   RequestedDate = {result.RequestedDate:yyyy-MM-dd}");
                Console.WriteLine($"[SmokeTest]   ActualRateDate= {result.ActualRateDate:yyyy-MM-dd}");
                Console.WriteLine($"[SmokeTest]   Source        = {result.Source}");
                Console.WriteLine($"[SmokeTest]   WasFetchedLive= {result.WasFetchedLive}");
                Console.WriteLine($"[SmokeTest]   IsStale       = {result.IsStale}");
                Console.WriteLine($"[SmokeTest]   (1 USD should = ~25.98 ZWG)");
                Console.WriteLine("----------------------------------------");
            }
            catch (Exception ex)
            {
                SmokeTestOutput = $"SMOKE TEST FAILED: {ex.GetType().Name} — {ex.Message}";
                Console.WriteLine($"[SmokeTest] EXCEPTION: {ex.GetType().Name} — {ex.Message}");
            }
        }
    }
}