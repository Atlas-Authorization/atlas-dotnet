using System;
using System.Numerics;
using System.Security.Cryptography;

namespace Atlas.Verification
{
    /// <summary>
    /// Ed25519 signature verification (RFC 8032 §5.1.7), for DPoP proofs signed with <c>EdDSA</c>: the
    /// .NET base class library has no Ed25519. Verification handles public data only, so it does not need
    /// to be constant-time.
    /// </summary>
    public static class Ed25519
    {
        internal static readonly BigInteger P = BigInteger.Pow(2, 255) - 19;
        internal static readonly BigInteger L = BigInteger.Pow(2, 252) + BigInteger.Parse("27742317777372353535851937790883648493");
        private static readonly BigInteger D = Mod(-121665 * Inv(121666));
        private static readonly BigInteger D2 = Mod(2 * D);
        private static readonly BigInteger SqrtM1 = BigInteger.ModPow(2, (P - 1) / 4, P);
        internal static readonly Point B = BasePoint();

        /// <summary>A point in extended coordinates (X:Y:Z:T), x = X/Z, y = Y/Z, xy = T/Z.</summary>
        internal readonly struct Point
        {
            public readonly BigInteger X, Y, Z, T;
            public Point(BigInteger x, BigInteger y, BigInteger z, BigInteger t) { X = x; Y = y; Z = z; T = t; }
        }

        /// <summary>Whether <paramref name="signature"/> (64 bytes) is a valid Ed25519 signature of <paramref name="message"/> under <paramref name="publicKey"/> (32 bytes).</summary>
        public static bool Verify(byte[] publicKey, byte[] message, byte[] signature)
        {
            if (publicKey == null || publicKey.Length != 32 || signature == null || signature.Length != 64 || message == null) return false;
            var a = Decode(publicKey);
            var rBytes = new byte[32];
            Array.Copy(signature, 0, rBytes, 0, 32);
            var r = Decode(rBytes);
            if (a == null || r == null) return false;
            var sBytes = new byte[32];
            Array.Copy(signature, 32, sBytes, 0, 32);
            var s = FromLe(sBytes);
            if (s >= L) return false;
            var k = Mod(FromLe(Sha512(rBytes, publicKey, message)), L);
            var left = Mul(s, B);
            var right = Add(r.Value, Mul(k, a.Value));
            return Equal(left, right);
        }

        internal static byte[] Sha512(params byte[][] parts)
        {
            using var h = SHA512.Create();
            foreach (var p in parts) h.TransformBlock(p, 0, p.Length, null, 0);
            h.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return h.Hash!;
        }

        internal static BigInteger Mod(BigInteger v, BigInteger? m = null)
        {
            var mod = m ?? P;
            var r = v % mod;
            return r.Sign < 0 ? r + mod : r;
        }

        private static BigInteger Inv(BigInteger v) => BigInteger.ModPow(Mod(v), P - 2, P);

        internal static BigInteger FromLe(byte[] le)
        {
            var buf = new byte[le.Length + 1];
            Array.Copy(le, buf, le.Length);
            return new BigInteger(buf);
        }

        internal static Point Add(Point p, Point q)
        {
            var a = Mod((p.Y - p.X) * (q.Y - q.X));
            var b = Mod((p.Y + p.X) * (q.Y + q.X));
            var c = Mod(p.T * D2 * q.T);
            var d = Mod(p.Z * 2 * q.Z);
            var e = b - a;
            var f = d - c;
            var g = d + c;
            var h = b + a;
            return new Point(Mod(e * f), Mod(g * h), Mod(f * g), Mod(e * h));
        }

        internal static Point Mul(BigInteger scalar, Point p)
        {
            var q = new Point(0, 1, 1, 0);
            var acc = p;
            while (scalar > 0)
            {
                if (!scalar.IsEven) q = Add(q, acc);
                acc = Add(acc, acc);
                scalar >>= 1;
            }
            return q;
        }

        private static bool Equal(Point p, Point q)
            => Mod(p.X * q.Z) == Mod(q.X * p.Z) && Mod(p.Y * q.Z) == Mod(q.Y * p.Z);

        /// <summary>The 32-byte encoding of a point.</summary>
        internal static byte[] Encode(Point p)
        {
            var zi = Inv(p.Z);
            var x = Mod(p.X * zi);
            var y = Mod(p.Y * zi);
            var bytes = y.ToByteArray();
            var out32 = new byte[32];
            Array.Copy(bytes, out32, Math.Min(32, bytes.Length));
            if (!x.IsEven) out32[31] |= 0x80;
            return out32;
        }

        private static Point? Decode(byte[] enc)
        {
            var buf = (byte[])enc.Clone();
            var sign = (buf[31] & 0x80) != 0;
            buf[31] &= 0x7f;
            var y = FromLe(buf);
            if (y >= P) return null;
            var x = RecoverX(y, sign);
            if (x == null) return null;
            return new Point(x.Value, y, 1, Mod(x.Value * y));
        }

        private static BigInteger? RecoverX(BigInteger y, bool sign)
        {
            var y2 = Mod(y * y);
            var x2 = Mod((y2 - 1) * Inv(D * y2 + 1));
            if (x2.IsZero) return sign ? (BigInteger?)null : BigInteger.Zero;
            var x = BigInteger.ModPow(x2, (P + 3) / 8, P);
            if (Mod(x * x - x2) != 0) x = Mod(x * SqrtM1);
            if (Mod(x * x - x2) != 0) return null;
            if (!x.IsEven != sign) x = P - x;
            return x;
        }

        private static Point BasePoint()
        {
            var y = Mod(4 * Inv(5));
            var x = RecoverX(y, false)!.Value;
            return new Point(x, y, 1, Mod(x * y));
        }
    }
}
