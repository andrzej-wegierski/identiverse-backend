using System.Text;
using Domain.Security;

namespace Tests.Domain.Security;

public class JwtKeyParserTests
{
    [Test]
    public void GetSigningKeyBytes_Returns_UTF8_For_PlainText()
    {
        var secret = "this-is-a-very-long-plain-text-secret-key";
        var expected = Encoding.UTF8.GetBytes(secret);
        
        var actual = JwtKeyParser.GetSigningKeyBytes(secret);
        
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetSigningKeyBytes_Returns_Decoded_Bytes_For_Base64()
    {
        // 32 bytes of random data encoded as base64
        var base64 = "YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXowMTIzNDU=";
        var expected = Convert.FromBase64String(base64);
        
        var actual = JwtKeyParser.GetSigningKeyBytes(base64);
        
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(actual.Length, Is.EqualTo(32));
    }

    [Test]
    public void GetSigningKeyBytes_Falls_Back_To_UTF8_If_Base64_Too_Short()
    {
        // "YWJj" is base64 for "abc" (3 bytes), which is < 32 bytes.
        var base64Short = "YWJj"; 
        var expected = Encoding.UTF8.GetBytes(base64Short);
        
        var actual = JwtKeyParser.GetSigningKeyBytes(base64Short);
        
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetSigningKeyBytes_Falls_Back_To_UTF8_If_Base64_Invalid()
    {
        var invalidBase64 = "ThisIsNotBase64!!!!";
        var expected = Encoding.UTF8.GetBytes(invalidBase64);
        
        var actual = JwtKeyParser.GetSigningKeyBytes(invalidBase64);
        
        Assert.That(actual, Is.EqualTo(expected));
    }
}
