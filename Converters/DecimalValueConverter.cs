using System.Numerics;
using Google.Protobuf;
using WolfBankGateway.Protos.Services;

namespace WolfBankGateway.Converters;

public static class DecimalValueConverter
{
  public static decimal ToDecimal(DecimalValue value)
  {
    var unscaled = new BigInteger(value.Value.ToByteArray());
    return (decimal)unscaled / (decimal)Math.Pow(10, value.Scale);
  }

  public static DecimalValue ToDecimalValue(decimal value)
  {
    var scale = BitConverter.GetBytes(decimal.GetBits(value)[3] >> 16)[0];
    var unscaled = new BigInteger(value * (decimal)Math.Pow(10, scale));

    return new DecimalValue
    {
      Value = ByteString.CopyFrom(unscaled.ToByteArray()),
      Scale = scale,
      Precision = (uint)unscaled.ToString().Length
    };
  }
}
