﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Model.Domain.PVs;
using VocaDb.Model.Domain.Songs;
using VocaDb.Model.Helpers;

namespace VocaDb.Model.Service.TagFormatting {

	public abstract class SongCsvFileFormatter<T> where T : ISongLink {

		private readonly IEntryLinkFactory entryLinkFactory;

		protected SongCsvFileFormatter(IEntryLinkFactory entryLinkFactory) {
			this.entryLinkFactory = entryLinkFactory;
		}

		protected string GetPvUrl(Song song, PVType? type, PVServices services) {
			var pv = song.PVs.FirstOrDefault(p => (type == null || p.PVType == type) && services.HasFlag((PVServices)p.Service));
			return pv != null ? pv.Url : string.Empty;
		}

		private string GetProducerStr(Song song, ContentLanguagePreference languagePreference) {

			return string.Join(", ", ArtistHelper.GetProducerNames(song.Artists, SongHelper.IsAnimation(song.SongType), languagePreference));

		}

		private string GetVocalistStr(Song song, ContentLanguagePreference languagePreference) {

			return string.Join(", ", ArtistHelper.GetVocalistNames(song.Artists, languagePreference));

		}

		protected string GetFieldValue(string fieldName, ISongLink songLink, ContentLanguagePreference languagePreference) {

			var song = songLink.Song;

			if (song == null)
				return string.Empty;

			switch (fieldName) {
				// Artists for song, both producers and vocalists
				case "artist":
					return song.ArtistString[languagePreference];
				case "track artist": // foobar style
					return song.ArtistString[languagePreference];

				// List of vocalists, separated by comma, with "feat." in the beginning if there are any vocalists, otherwise empty.
				case "featvocalists":
					var vocalistStr = GetVocalistStr(song, languagePreference);
					return (vocalistStr.Any() ? " feat. " + vocalistStr : string.Empty);

				// List of producers
				case "producers":
					return GetProducerStr(song, languagePreference);

				case "publishdate":
					return song.PublishDate.DateTime.HasValue ? song.PublishDate.ToString() : string.Empty;
				case "pv.original.niconicodouga":
					return GetPvUrl(song, PVType.Original, PVServices.NicoNicoDouga);
				case "pv.original.!niconicodouga":
					return GetPvUrl(song, PVType.Original, (PVServices)(EnumVal<PVServices>.All - PVServices.NicoNicoDouga));
				case "pv.reprint":
					return GetPvUrl(song, PVType.Reprint, EnumVal<PVServices>.All);
				case "title":
					return song.Names.SortNames[languagePreference];
				case "url":
					return entryLinkFactory.GetFullEntryUrl(EntryType.Song, song.Id);

				// List of vocalists, separated by comma.
				case "vocalists":
					return GetVocalistStr(song, languagePreference);

				default:
					return string.Empty;
			}

		}

		protected virtual string GetFieldValue(string fieldName, T track, ContentLanguagePreference languagePreference) {
			return GetFieldValue(fieldName, (ISongLink)track, languagePreference);
		}

		private void ReplaceField(
			string tokenName, string tokenStr, StringBuilder sb, T track, ContentLanguagePreference languagePreference) {

			var val = GetField(GetFieldValue(tokenName.ToLowerInvariant(), track, languagePreference));
			sb.Replace(tokenStr, val);

		}

		private string ApplyFormat(T track, string format, ContentLanguagePreference languagePreference, MatchCollection fieldMatches) {

			var sb = new StringBuilder(format);

			foreach (Match match in fieldMatches) {
				ReplaceField(match.Groups[1].Value, match.Value, sb, track, languagePreference);
			}

			return sb.ToString();

		}

		private static string GetField(string val) {

			if (string.IsNullOrEmpty(val))
				return string.Empty;

			if (!val.Contains(";"))
				return val;
			else
				return string.Format("\"{0}\"", val);

		}

		protected string ApplyFormat(IEnumerable<T> songs, string format, ContentLanguagePreference languagePreference, bool includeHeader) {

			var sb = new StringBuilder();

			var fieldRegex = new Regex(@"%([\w\.!]+)%");
			var formatFields = fieldRegex.Matches(format);

			if (includeHeader) {
				sb.AppendLine(string.Join(";", formatFields.Cast<Match>().Select(f => GetField(f.Groups[1].Value))));
			}

			foreach (var song in songs)
				sb.AppendLine(ApplyFormat(song, format, languagePreference, formatFields));

			return sb.ToString();

		}

	}
}