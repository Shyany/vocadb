using System;
using VocaDb.Model.Utils.Config;

namespace VocaDb.Model.Service.Helpers {

	public static class UrlHelper {

		private static bool IsFullLink(string str) {

			return (str.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
				|| str.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)
				|| str.StartsWith("mailto:", StringComparison.InvariantCultureIgnoreCase));

		}

		/// <summary>
		/// Makes a proper URL from a possible URL without a http:// prefix.
		/// </summary>
		/// <param name="partialLink">Partial URL. Can be null.</param>
		/// <param name="assumeWww">Whether to assume the URL should start with www.</param>
		/// <returns>Full URL including http://. Can be null if source was null.</returns>
		public static string MakeLink(string partialLink, bool assumeWww = false) {

			if (string.IsNullOrEmpty(partialLink))
				return partialLink;

			if (IsFullLink(partialLink))
				return partialLink;

			if (assumeWww && !partialLink.StartsWith("www.", StringComparison.InvariantCultureIgnoreCase))
				return string.Format("http://www.{0}", partialLink);

			return string.Format("http://{0}", partialLink);

		}

		public static string MakePossileAffiliateLink(string partialLink) {

			var link = MakeLink(partialLink);

			return (new ExtSites.AffiliateLinkGenerator(new VdbConfigManager())).GenerateAffiliateLink(link);

		}
		
		/// <summary>
		/// Removes http:// and https:// from the beginning of an URL.
		/// </summary>
		public static string RemoveScheme(string url) {

			if (string.IsNullOrEmpty(url))
				return url;

			if (url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)) {
				return url.Substring(7);
			} else if (url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)) {
				return url.Substring(8);
			}

			return url;

		}

		public static string UpgradeToHttps(string url) {

			if (string.IsNullOrEmpty(url))
				return url;

			if (url.StartsWith("http://tn.smilevideo.jp/smile")) {
				return url.Replace("http://", "https://");
			}

			return url;

		}

	}

}
