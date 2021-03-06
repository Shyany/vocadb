﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Artists;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Model.Domain.PVs;
using VocaDb.Model.Domain.Songs;
using VocaDb.Model.Helpers;
using VocaDb.Model.Service.Search;
using VocaDb.Model.Service.Search.AlbumSearch;

namespace VocaDb.Model.Service.QueryableExtenders {

	public static class SongQueryableExtender {

		public static IQueryable<Song> OrderByPublishDate(this IQueryable<Song> criteria, SortDirection direction) {

			return criteria.OrderBy(a => a.PublishDate, direction)
				.ThenBy(a => a.CreateDate, direction);

		}

		public static IQueryable<Song> OrderBy(this IQueryable<Song> query, SongSortRule sortRule, ContentLanguagePreference languagePreference = ContentLanguagePreference.Default) {
			
			switch (sortRule) {
				case SongSortRule.Name:
					return query.OrderByEntryName(languagePreference);
				case SongSortRule.AdditionDate:
					return query.OrderByDescending(a => a.CreateDate);
				case SongSortRule.FavoritedTimes:
					return query.OrderByDescending(a => a.FavoritedTimes);
				case SongSortRule.PublishDate:
					return query.OrderByPublishDate(SortDirection.Descending);
				case SongSortRule.RatingScore:
					return query.OrderByDescending(a => a.RatingScore);
			}

			return query;

		}

		public static IQueryable<Song> OrderBy(
			this IQueryable<Song> query, EntrySortRule sortRule, ContentLanguagePreference languagePreference) {

			switch (sortRule) {
				case EntrySortRule.Name:
					return query.OrderByEntryName(languagePreference);
				case EntrySortRule.AdditionDate:
					return query.OrderByDescending(a => a.CreateDate);
				case EntrySortRule.ActivityDate:
					return query.OrderByDescending(a => a.PublishDate.DateTime);
			}

			return query;

		}

		public static IQueryable<Song> WhereArtistHasTag(this IQueryable<Song> query, string tagName) {

			if (string.IsNullOrEmpty(tagName))
				return query;

			return query.Where(s => s.AllArtists.Any(a => a.Artist.Tags.Usages.Any(t => t.Tag.Names.SortNames.English == tagName || t.Tag.Names.SortNames.Romaji == tagName || t.Tag.Names.SortNames.Japanese == tagName)));

		}

		public static IQueryable<Song> WhereArtistHasType(this IQueryable<Song> query, ArtistType artistType) {
			return query.WhereArtistHasType<Song, ArtistForSong>(artistType);
		}

		public static IQueryable<Song> WhereArtistIsFollowedByUser(this IQueryable<Song> query, int userId) {

			if (userId == 0)
				return query;

			query = query.Where(s => s.AllArtists.Any(a => a.Artist.Users.Any(u => u.User.Id == userId)));

			return query;

		}

		public static IQueryable<Song> WhereDraftsOnly(this IQueryable<Song> query, bool draftsOnly) {

			if (!draftsOnly)
				return query;

			return query.Where(a => a.Status == EntryStatus.Draft);

		}

		/// <summary>
		/// Filters a song query by a list of artist Ids.
		/// </summary>
		public static IQueryable<Song> WhereHasArtist(this IQueryable<Song> query, int artistId) {

			if (artistId == 0)
				return query;

			return query.WhereHasArtist<Song, ArtistForSong>(artistId, false, false);

		}

		public static IQueryable<Song> WhereHasArtistParticipationStatus(this IQueryable<Song> query, 
			ArtistParticipationQueryParams queryParams,
			Func<int, Artist> artistGetter) {

			var various = Model.Helpers.ArtistHelper.VariousArtists;
			var producerRoles = ArtistRoles.Composer | ArtistRoles.Arranger;
			var artistId = queryParams.ArtistIds.Primary;

			return EntryWithArtistsQueryableExtender.WhereHasArtistParticipationStatus(new ArtistParticipationQueryParams<Song, ArtistForSong>(query, queryParams, artistGetter,
				al => al.AllArtists.Any(a => a.Artist.Id == artistId && !a.IsSupport && ((a.Roles == ArtistRoles.Default) || ((a.Roles & producerRoles) != ArtistRoles.Default)) && a.Song.ArtistString.Default != various),
				al => al.AllArtists.Any(a => a.Artist.Id == artistId && (a.IsSupport || ((a.Roles != ArtistRoles.Default) && ((a.Roles & producerRoles) == ArtistRoles.Default)) || a.Song.ArtistString.Default == various))
			));

		}

		public static IQueryable<Song> WhereHasLyrics(this IQueryable<Song> query, string[] languageCodes, bool any) {

			if (any) {
				return query.Where(s => s.Lyrics.Any());
			} else if (languageCodes != null && languageCodes.Any()) {
				return query.Where(s => s.Lyrics.Any(l => languageCodes.Contains(l.CultureCode.CultureCode)));
			} else {
				return query;
			}

		} 

		public static IQueryable<Song> WhereHasLyricsContent(this IQueryable<Song> query, string text) {

			if (string.IsNullOrEmpty(text))
				return query;

			return query.Where(s => s.Lyrics.Any(l => l.Value.Contains(text)));

		}

		/// <summary>
		/// Filters a song query by a name query.
		/// </summary>
		/// <param name="query">Song query. Cannot be null.</param>
		/// <param name="nameFilter">Name filter string. If null or empty, no filtering is done.</param>
		/// <param name="matchMode">Desired mode for matching names.</param>
		/// <param name="words">
		/// List of words for the words search mode. 
		/// Can be null, in which case the words list will be parsed from <paramref name="nameFilter"/>.
		/// </param>
		/// <returns>Filtered query. Cannot be null.</returns>
		public static IQueryable<Song> WhereHasName(this IQueryable<Song> query, SearchTextQuery textQuery) {

			return query.WhereHasNameGeneric<Song, SongName>(textQuery);

		}

		/// <summary>
		/// Filters a song link query by a name query.
		/// </summary>
		/// <param name="query">Song query. Cannot be null.</param>
		/// <param name="nameFilter">Name filter string. If null or empty, no filtering is done.</param>
		/// <param name="matchMode">Desired mode for matching names.</param>
		/// <param name="words">
		/// List of words for the words search mode. 
		/// Can be null, in which case the words list will be parsed from <paramref name="nameFilter"/>.
		/// </param>
		/// <returns>Filtered query. Cannot be null.</returns>
		public static IQueryable<T> WhereChildHasName<T>(this IQueryable<T> query, SearchTextQuery textQuery) where T : ISongLink {

			if (textQuery.IsEmpty)
				return query;

			var nameFilter = textQuery.Query;

			switch (textQuery.MatchMode) {
				case NameMatchMode.Exact:
					return query.Where(m => m.Song.Names.Names.Any(n => n.Value == nameFilter));

				case NameMatchMode.Partial:
					return query.Where(m => m.Song.Names.Names.Any(n => n.Value.Contains(nameFilter)));

				case NameMatchMode.StartsWith:
					return query.Where(m => m.Song.Names.Names.Any(n => n.Value.StartsWith(nameFilter)));

				case NameMatchMode.Words:
					var words = textQuery.Words;

					Expression<Func<T, bool>> exp = (q => q.Song.Names.Names.Any(n => n.Value.Contains(words[0])));

					foreach (var word in words.Skip(1).Take(10)) {
						var temp = word;
						exp = exp.And((q => q.Song.Names.Names.Any(n => n.Value.Contains(temp))));
					}

					return query.Where(exp);

			}

			return query;

		}

		public static IQueryable<Song> WhereCreateDateIsWithin(this IQueryable<Song> criteria, TimeSpan timeFilter) {

			if (timeFilter == TimeSpan.Zero)
				return criteria;

			var since = DateTime.Now - timeFilter;

			return criteria.Where(t => t.CreateDate >= since);

		}

		public static IQueryable<Song> WhereHasNicoId(this IQueryable<Song> query, string nicoId) {

			if (string.IsNullOrEmpty(nicoId))
				return query;

			return query.Where(s => s.NicoId == nicoId);

		}

		public static IQueryable<Song> WhereHasPublishDate(this IQueryable<Song> query, bool hasPublishDate) {

			return hasPublishDate ? query.Where(s => s.PublishDate.DateTime != null) : query.Where(s => s.PublishDate.DateTime == null);

		}

		/// <summary>
		/// Filter query by PV services bit array.
		/// Song will pass the filter if ANY of the specified PV services matches.
		/// </summary>
		/// <param name="query">Query. Cannot be null.</param>
		/// <param name="pvServices">PV services bit array. Can be null, in which case no filtering will be done.</param>
		/// <returns>Filtered query.</returns>
		public static IQueryable<Song> WhereHasPVService(this IQueryable<Song> query, PVServices? pvServices) {
			
			if (pvServices == null || pvServices.Value == PVServices.Nothing)
				return query;

			return query.Where(s => (s.PVServices & pvServices) != PVServices.Nothing);

		} 

		public static IQueryable<Song> WhereHasPV(this IQueryable<Song> criteria, bool onlyWithPVs) {

			if (onlyWithPVs)
				return criteria.Where(t => t.PVServices != PVServices.Nothing);
			else
				return criteria;

		}

		public static IQueryable<Song> WhereHasPV(this IQueryable<Song> query, PVService? service, string pvId) {

			if (service == null || pvId == null)
				return query;

			return query.Where(s => s.PVs.PVs.Any(pv => pv.Service == service && pv.PVId == pvId));

		}

		public static IQueryable<Song> WhereHasPV(this IQueryable<Song> query, IPV pv) {

			if (pv == null)
				return query;

			return WhereHasPV(query, pv.Service, pv.PVId);

		}

		public static IQueryable<Song> WhereHasScore(this IQueryable<Song> query, int minScore) {

			if (minScore <= 0)
				return query;

			return query.Where(q => q.RatingScore >= minScore);

		}

		public static IQueryable<Song> WhereHasTag(this IQueryable<Song> query, string tagName) {

			return query.WhereHasTag<Song, SongTagUsage>(tagName);

		}

		public static IQueryable<Song> WhereHasTags(this IQueryable<Song> query, string[] tagName) {

			return query.WhereHasTags<Song, SongTagUsage>(tagName);

		}

		public static IQueryable<Song> WhereHasTags(this IQueryable<Song> query, int[] tagId, bool childTags = false) {

			return query.WhereHasTags<Song, SongTagUsage>(tagId, childTags);

		}

		public static IQueryable<Song> WhereHasType(this IQueryable<Song> query, SongType[] songTypes) {

			if (!songTypes.Any())
				return query;

			return query.Where(m => songTypes.Contains(m.SongType));

		}

		public static IQueryable<Song> WhereHasVocalist(this IQueryable<Song> query, SongVocalistSelection vocalist) {

			switch (vocalist) {
				case SongVocalistSelection.Vocaloid:
					return query.Where(s => s.AllArtists.Any(a => !a.IsSupport && a.Artist.ArtistType == ArtistType.Vocaloid));
				case SongVocalistSelection.UTAU:
					return query.Where(s => s.AllArtists.Any(a => !a.IsSupport && a.Artist.ArtistType == ArtistType.UTAU));
				case SongVocalistSelection.CeVIO:
					return query.Where(s => s.AllArtists.Any(a => !a.IsSupport && (a.Artist.ArtistType == ArtistType.CeVIO || a.Artist.ArtistType == ArtistType.OtherVoiceSynthesizer)));
			}

			return query;

		}

		public static IQueryable<Song> WhereIdIs(this IQueryable<Song> query, int id) {
			
			if (id == 0)
				return query;

			return query.Where(m => m.Id == id);

		} 

		public static IQueryable<Song> WhereIdNotIn(this IQueryable<Song> query, int[] ignoreIds) {

			if (ignoreIds == null || !ignoreIds.Any())
				return query;

			return query.Where(s => !ignoreIds.Contains(s.Id));

		} 

		public static IQueryable<Song> WhereInUserCollection(this IQueryable<Song> query, int userId) {

			if (userId == 0)
				return query;

			query = query.Where(s => s.UserFavorites.Any(a => a.User.Id == userId));

			return query;

		}

		public static IQueryable<Song> WhereMatchFilter(this IQueryable<Song> query, AdvancedSearchFilter filter) {

			if (filter == null)
				return query;

			switch (filter.FilterType) {
				case AdvancedFilterType.ArtistType: {
					var param = EnumVal<ArtistType>.Parse(filter.Param);
					return WhereArtistHasType(query, param);
				}
				case AdvancedFilterType.HasAlbum:
					return filter.Negate ? query.Where(s => !s.AllAlbums.Any()) : query.Where(s => s.AllAlbums.Any());
				case AdvancedFilterType.HasMultipleVoicebanks: {
					return query.Where(s => s.AllArtists.Count(a => !a.IsSupport && ArtistHelper.VoiceSynthesizerTypes.Contains(a.Artist.ArtistType)) > 1);	
				}
				case AdvancedFilterType.HasPublishDate: {
					return query.WhereHasPublishDate(!filter.Negate);
				}
				case AdvancedFilterType.Lyrics: {
					var any = filter.Param == AdvancedSearchFilter.Any;
					var languageCodes = !any ? (filter.Param ?? string.Empty).Split(',') : null;
					return WhereHasLyrics(query, languageCodes, any);
				}
				case AdvancedFilterType.LyricsContent: {
					return query.WhereHasLyricsContent(filter.Param);
				}
				case AdvancedFilterType.HasOriginalMedia: {
					return query.Where(s => filter.Negate != s.PVs.PVs.Any(pv => !pv.Disabled && pv.PVType == PVType.Original));
				}
				case AdvancedFilterType.HasMedia: {
					return query.Where(s => filter.Negate != s.PVs.PVs.Any());
				}
				case AdvancedFilterType.WebLink: {
					return query.WhereHasLink<Song, SongWebLink>(filter.Param);
				}
			}

			return query;

		}

		public static IQueryable<Song> WhereMatchFilters(this IQueryable<Song> query, IEnumerable<AdvancedSearchFilter> filters) {

			return filters?.Aggregate(query, WhereMatchFilter) ?? query;

		}

		public static IQueryable<Song> WherePublishDateIsBetween(this IQueryable<Song> query, DateTime? begin, DateTime? end) {

			if (begin.HasValue && end.HasValue)
				return query.Where(e => e.PublishDate.DateTime != null && e.PublishDate.DateTime >= begin && e.PublishDate.DateTime < end);

			if (begin.HasValue)
				return query.Where(e => e.PublishDate.DateTime != null && e.PublishDate.DateTime >= begin);

			if (end.HasValue)
				return query.Where(e => e.PublishDate.DateTime != null && e.PublishDate.DateTime < end);

			return query;

		}

		public static IQueryable<Song> WhereReleaseEventIs(this IQueryable<Song> query, int eventId) {

			if (eventId == 0)
				return query;

			return query.Where(s => s.ReleaseEvent.Id == eventId);

		}

	}

	public enum SongVocalistSelection {
		Nothing,
		Vocaloid,
		UTAU,
		CeVIO
	}

}
