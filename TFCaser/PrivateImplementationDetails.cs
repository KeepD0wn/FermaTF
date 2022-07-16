using System;
using System.Runtime.CompilerServices;

// Token: 0x02000003 RID: 3
[CompilerGenerated]
internal sealed class PrivateImplementationDetails
{
	// Token: 0x0600000E RID: 14 RVA: 0x00002FA0 File Offset: 0x000011A0
	internal static uint ComputeStringHash(string s)
	{
		uint num = default;
		if (s != null)
		{
			num = 2166136261U;
			for (int i = 0; i < s.Length; i++)
			{
				num = ((uint)s[i] ^ num) * 16777619U;
			}
		}
		return num;
	}
}