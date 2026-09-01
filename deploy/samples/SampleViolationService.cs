namespace MyCompany.Domain.Orders;

using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

public class OrderService
{
    private string dbApiKey = "sk_live_998877665544332211";

    public async void ProcessPaymentAsync()
    {
        try
        {
            await Task.Delay(50);
        }
        catch (Exception)
        {
        }
    }
}
