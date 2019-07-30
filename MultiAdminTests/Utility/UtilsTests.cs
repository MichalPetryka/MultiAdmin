using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiAdmin.Utility;

namespace MultiAdminTests.Utility
{
	[TestClass]
	public class UtilsTests
	{
		private struct StringMatchingTemplate
		{
			public readonly string input;
			public readonly string pattern;

			public readonly bool expectedResult;

			public StringMatchingTemplate(string input, string pattern, bool expectedResult)
			{
				this.input = input;
				this.pattern = pattern;
				this.expectedResult = expectedResult;
			}
		}

		[TestMethod]
		public void GetFullPathSafeTest()
		{
			string result = Utils.GetFullPathSafe(" ");
			Assert.IsNull(result, $"Expected \"null\", got \"{result}\"");
		}

		[TestMethod]
		public void StringMatchesTest()
		{
			StringMatchingTemplate[] matchTests =
			{
				new StringMatchingTemplate("test", "*", true),
				new StringMatchingTemplate("test", "te*", true),
				new StringMatchingTemplate("test", "*st", true),
				new StringMatchingTemplate("test", "******", true),
				new StringMatchingTemplate("test", "te*t", true),
				new StringMatchingTemplate("test", "t**st", true),
				new StringMatchingTemplate("test", "s*", false),
				new StringMatchingTemplate("longstringtestmessage", "l*s*t*e*g*", true),
			};

			for (int i = 0; i < matchTests.Length; i++)
			{
				StringMatchingTemplate test = matchTests[i];

				bool result = Utils.StringMatches(test.input, test.pattern);

				Assert.IsTrue(test.expectedResult == result, $"Failed on test index {i}: Expected \"{test.expectedResult}\", got \"{result}\"");
			}
		}
	}
}
