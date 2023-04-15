namespace JUST
{
    public class CustomFunction
    {
        public string AssemblyName { get; set; }
        public string Namespace { get; set; }
        public string MethodName { get; set; }
        public string MethodAlias { get; set; }

        public CustomFunction()
        {
        }

        public CustomFunction(string assemblyName, string namespc, string methodName, string methodAlias = null)
        {
            AssemblyName = assemblyName;
            Namespace = namespc;
            MethodName = methodName;
            MethodAlias = methodAlias;
        }
    }
}