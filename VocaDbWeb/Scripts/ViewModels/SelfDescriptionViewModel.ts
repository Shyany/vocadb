﻿
namespace vdb.viewModels {

	export class SelfDescriptionViewModel {

		constructor(author: dc.ArtistApiContract, text: string, artistRepo: rep.ArtistRepository, private getArtists: (callback: (result: dc.ArtistContract[]) => void) => void,
			private saveFunc: ((vm: SelfDescriptionViewModel) => void)) {
			
			this.author = new BasicEntryLinkViewModel<dc.ArtistApiContract>(author, (artistId, callback) => {
				artistRepo.getOneWithComponents(artistId, 'MainPicture', artist => {
					callback(artist);
				});
			});
			this.text = ko.observable(text);

		}

		public artists = ko.observableArray<dc.ArtistContract>();

		public author: BasicEntryLinkViewModel<dc.ArtistApiContract>;

		public beginEdit = () => {

			this.originalAuthor = this.author.id();
			this.originalText = this.text();

			if (!this.artists().length) {
				this.getArtists(artists => {
					this.artists(artists);
					this.editing(true);
				});
			} else {
				this.editing(true);				
			}

		}

		public cancelEdit = () => {
			this.text(this.originalText);
			this.author.id(this.originalAuthor);
			this.editing(false);
		};

		public editing = ko.observable(false);

		private originalAuthor: number;
		private originalText: string;

		public save = () => {
			this.saveFunc(this);
			this.editing(false);
		}

		public text: KnockoutObservable<string>;

	}

}