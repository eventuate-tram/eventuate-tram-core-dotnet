/*
 * Ported from:
 * repo:	https://github.com/eventuate-clients/eventuate-client-java
 * module:	eventuate-client-java
 * package:	io.eventuate
 */

using System;
using System.Globalization;

namespace IO.Eventuate.Tram
{
	public class Int128 : IComparable<Int128>, IComparable
	{
		private readonly long _hi;
		private readonly long _lo;

		public Int128(long hi, long lo) {
			this._hi = hi;
			this._lo = lo;
		}

		public string AsString() {
			return $"{_hi:x16}-{_lo:x16}";
		}

		public override string ToString() {
			return "Int128{" + AsString() + '}';
		}

		protected bool Equals(Int128 other)
		{
			return _hi == other._hi && _lo == other._lo;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}
			
			return Equals((Int128) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (_hi.GetHashCode() * 397) ^ _lo.GetHashCode();
			}
		}

		public static Int128 FromString(string str) {
			string[] s = str.Split('-');
			if (s.Length != 2)
			{
				throw new ArgumentException("Should have length of 2: " + str);
			}

			// NumberStyles.HexNumber does not allow leading sign so it is equivalent
			// to the Java version uses parseUnsignedLong.
			return new Int128(Int64.Parse(s[0], NumberStyles.HexNumber), Int64.Parse(s[1], NumberStyles.HexNumber));
		}

		public long Hi => _hi;

		public long Lo => _lo;

		public int CompareTo(Int128 other)
		{
			if (ReferenceEquals(this, other)) return 0;
			if (ReferenceEquals(null, other)) return 1;
			int hiComparison = _hi.CompareTo(other._hi);
			if (hiComparison != 0) return hiComparison;
			return _lo.CompareTo(other._lo);
		}

		public int CompareTo(object obj)
		{
			if (ReferenceEquals(null, obj)) return 1;
			if (ReferenceEquals(this, obj)) return 0;
			return obj is Int128 other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Int128)}");
		}
	}
}