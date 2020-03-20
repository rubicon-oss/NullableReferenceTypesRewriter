namespace NUnit2To3SyntaxConverter.Unittests
{
  public class MethodArguments
  {
    private const string c_template = "%TEMPLATE%";
    private const string c_argumentTemplate =
@"public class ArgumentTest
{
  public void TestMethod (int structType, object referenceType1, decimal structType2, string referenceType2)
  {
  }

  public void CallSite ()
  {
    %TEMPLATE%;
  }
}";

    

  }
}