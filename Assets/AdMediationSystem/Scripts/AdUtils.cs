
using System.Security.Cryptography;
using System.Text;

namespace Virterix {
    namespace AdMediation {

        public static class AdUtils {

            public static string GetHash(string text) {
                MD5 md5 = MD5.Create();
                byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
                string hashStr = Encoding.ASCII.GetString(hash);
                return hashStr;
            }

        }

    } // namespace AdMediation
} // namespace Virterix