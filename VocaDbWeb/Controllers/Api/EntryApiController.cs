﻿using System;
using System.Web.Http;
using System.Web.Http.Description;
using VocaDb.Model.Database.Queries;
using VocaDb.Model.DataContracts.Api;
using VocaDb.Model.Domain;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Model.Service;
using VocaDb.Model.Service.Search;
using VocaDb.Web.Code.Exceptions;
using VocaDb.Web.Helpers;

namespace VocaDb.Web.Controllers.Api {

	/// <summary>
	/// Controller for managing base class for common entries.
	/// </summary>
	[RoutePrefix("api/entries")]
	public class EntryApiController : ApiController {

		private const int absoluteMax = 50;
		private const int defaultMax = 10;

		private readonly AlbumService albumService;
		private readonly ArtistService artistService;
		private readonly IEntryUrlParser entryUrlParser;
		private readonly EntryQueries queries;
		private readonly OtherService otherService;
		private readonly SongQueries songQueries;

		private int GetMaxResults(int max) {
			return Math.Min(max, absoluteMax);	
		}

		public EntryApiController(EntryQueries queries, OtherService otherService, AlbumService albumService, ArtistService artistService, SongQueries songQueries, IEntryUrlParser entryUrlParser) {
			this.queries = queries;
			this.otherService = otherService;
			this.albumService = albumService;
			this.artistService = artistService;
			this.songQueries = songQueries;
			this.entryUrlParser = entryUrlParser;
		}

		/// <summary>
		/// Find entries.
		/// </summary>
		/// <param name="query">Entry name query (optional).</param>
		/// <param name="tagName">Filter by tag name (optional).</param>
		/// <param name="tagId">Filter by tag Id (optional).</param>
		/// <param name="childTags">Include child tags, if the tags being filtered by have any.</param>
		/// <param name="status">Filter by entry status (optional).</param>
		/// <param name="start">First item to be retrieved (optional, defaults to 0).</param>
		/// <param name="maxResults">Maximum number of results to be loaded (optional, defaults to 10, maximum of 30).</param>
		/// <param name="getTotalCount">Whether to load total number of items (optional, default to false).</param>
		/// <param name="sort">Sort rule (optional, defaults to Name). Possible values are None, Name, AdditionDate.</param>
		/// <param name="nameMatchMode">Match mode for entry name (optional, defaults to Exact).</param>
		/// <param name="fields">List of optional fields (optional). Possible values are Description, MainPicture, Names, Tags, WebLinks.</param>
		/// <param name="lang">Content language preference (optional).</param>
		/// <returns>Page of entries.</returns>
		/// <example>http://vocadb.net/api/entries?query=164&amp;fields=MainPicture</example>
		[Route("")]
		public PartialFindResult<EntryForApiContract> GetList(
			string query = "",
			[FromUri] string[] tagName = null,
			[FromUri] int[] tagId = null,
			bool childTags = false,
			EntryStatus? status = null,
			int start = 0, int maxResults = defaultMax, bool getTotalCount = false,
			EntrySortRule sort = EntrySortRule.Name,
			NameMatchMode nameMatchMode = NameMatchMode.Exact,
			EntryOptionalFields fields = EntryOptionalFields.None,
			ContentLanguagePreference lang = ContentLanguagePreference.Default
			) {
			
			var ssl = WebHelper.IsSSL(Request);
			maxResults = GetMaxResults(maxResults);

			return queries.GetList(query, tagId, tagName, childTags, status, start, maxResults, getTotalCount, sort, nameMatchMode, fields, lang, ssl, searchEvents: true);

		}

		/// <summary>
		/// Gets a list of entry names. Ideal for autocomplete boxes.
		/// </summary>
		/// <param name="query">Text query.</param>
		/// <param name="nameMatchMode">Name match mode.</param>
		/// <param name="maxResults">Maximum number of results.</param>
		/// <returns>List of entry names.</returns>
		[Route("names")]
		public string[] GetNames(string query = "", NameMatchMode nameMatchMode = NameMatchMode.Auto, int maxResults = 10) {
			
			return otherService.FindNames(SearchTextQuery.Create(query, nameMatchMode), maxResults);

		}

		[ApiExplorerSettings(IgnoreApi = true)]
		[Route("tooltip")]
		public string GetToolTip(string url) {

			if (string.IsNullOrWhiteSpace(url)) {
				throw new HttpBadRequestException("URL must be specified");
			}

			var entryId = entryUrlParser.Parse(url, allowRelative: true);

			if (entryId.IsEmpty) {
				throw new HttpBadRequestException("Invalid URL");
			}

			var data = string.Empty;
			var id = entryId.Id;

			switch (entryId.EntryType) {
				case EntryType.Album:
					data = RazorHelper.RenderPartialViewToString("AlbumWithCoverPopupContent", albumService.GetAlbum(id), "EntryApiController", Request);
					break;
				case EntryType.Artist:
					data = RazorHelper.RenderPartialViewToString("ArtistPopupContent", artistService.GetArtist(id), "EntryApiController", Request);
					break;
				case EntryType.Song:
					data = RazorHelper.RenderPartialViewToString("SongPopupContent", songQueries.GetSong(id), "EntryApiController", Request);
					break;
			}

			return data;

		}

	}

}