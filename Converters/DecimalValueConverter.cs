using System.Numerics;
using Google.Protobuf;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Converters;

public static class DecimalValueConverter
{
  public static decimal ToDecimal(DecimalValue decimalValue)
  {
    return decimalValue.Unscaled / (decimal)Math.Pow(10, decimalValue.Scale);
  }

  public static DecimalValue ToDecimalValue(decimal value)
  {
    var scale = BitConverter.GetBytes(decimal.GetBits(value)[3] >> 16)[0];
    var unscaled = value * (decimal)Math.Pow(10, scale);

    return new DecimalValue
    {
      Unscaled = (long)unscaled,
      Scale = scale
    };
  }
}
