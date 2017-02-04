using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Collections;
using System.Text;
using System.IO;


namespace Virterix {
    namespace Common {

        public struct SecureValue<T> {
            string m_value;
            string m_key;

            public SecureValue(T value, string key = "") {
                m_key = key;
                m_value = CryptString.Encode(value.ToString(), m_key);
            }

            public T Value {
                get {
                    string decodeValue = CryptString.Decode(m_value, m_key);
                    return (T)System.Convert.ChangeType(decodeValue, typeof(T));
                }
                set {
                    m_value = CryptString.Encode(value.ToString(), m_key);
                }
            }

            public string Secure {
                get {
                    return m_value;
                }
            }
        }

        public class CryptString {

            public static string Encode(string text, string key = "") {
                string encodeString = "";
                try {
                    byte[] textBytes;
                    if (key.Length > 0) {
                        byte[] keyByteArr = Encoding.UTF8.GetBytes(key);
                        textBytes = EncodeByte(Encoding.UTF8.GetBytes(text), keyByteArr);
                    } else {
                        textBytes = Encoding.UTF8.GetBytes(text);
                    }
                    encodeString = System.Convert.ToBase64String(textBytes);
                } catch {
                    encodeString = "";
                }
                return encodeString;
            }

            public static string Decode(string text, string key = "") {
                string decodeString = "";
                try {
                    byte[] textBytes;
                    if (key.Length > 0) {
                        byte[] keyByteArr = Encoding.UTF8.GetBytes(key);
                        textBytes = EncodeByte(System.Convert.FromBase64String(text), keyByteArr);
                    } else {
                        textBytes = System.Convert.FromBase64String(text);
                    }
                    decodeString = Encoding.UTF8.GetString(textBytes);
                } catch {
                    decodeString = "";
                }
                return decodeString;
            }

            private static byte[] EncodeByte(byte[] bytes, byte[] key) {
                var j = 0;
                for (var i = 0; i < bytes.Length; i++) {
                    bytes[i] ^= key[j];
                    if (++j == key.Length) {
                        j = 0;
                    }
                }
                return bytes;
            }

        }

    } // namespace Common
} // namespace Virterix


