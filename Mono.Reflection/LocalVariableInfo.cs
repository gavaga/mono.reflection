using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Reflection
{
    public class LocalVariableInfo : IEquatable<LocalVariableInfo>{
		public bool IsPinned { get; }
		public int LocalIndex { get; }
		public Type LocalType { get; }

		public LocalVariableInfo(int index, Type type, bool isPinned = false) {
			IsPinned = isPinned;
			LocalIndex = index;
			LocalType = type;
		}

		public bool Equals(LocalVariableInfo other) {
			return IsPinned == other.IsPinned
				&& LocalIndex == other.LocalIndex
				&& LocalType.Equals(other.LocalType);
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(null, obj)) return false;
			if (object.ReferenceEquals(this, obj)) return true;
			return Equals(obj as LocalVariableInfo);
		}

		public override int GetHashCode() {
			return (((IsPinned.GetHashCode() * 397) 
				^ LocalIndex.GetHashCode()) * 397)
				^ LocalType.GetHashCode();
		}
	}
}
