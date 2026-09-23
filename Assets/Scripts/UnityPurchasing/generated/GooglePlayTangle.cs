// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("FE78Cl7/kkyVPK15PjybNL4YNlRXFKz/mlQfFDz+Cl24EJzbkC6+nCQZkJSYDhS8");
        private static int[] order = new int[] { 0,1,2 };
        private static int key = 227;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
