namespace WolfBankGateway.Models;

public class PaymentModel
{
  public decimal Amount { get; set; }
  public string? ToBankAccountId { get; set; }
}
