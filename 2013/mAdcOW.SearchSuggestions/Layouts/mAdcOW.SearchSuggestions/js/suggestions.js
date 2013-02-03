ExecuteOrDelayUntilScriptLoaded(Override, 'ajaxtoolkit.js');
function Override() {
    AjaxControlToolkit.AutoCompleteBehavior.prototype._getSuggestion = function() {
        var rootCall = this;
        var cctx = Srch.ScriptApplicationManager.get_clientRuntimeContext();
        try {
            var text = this._currentPrefix;
            var lang = Srch.AU.get_querySuggestionLanguage();
            var sendInfo = {
                url: cctx.get_url(),
                sourceId: this._sourceId,
                query: this._currentPrefix,
                language: lang,
                numberOfQuerySuggestions: this._queryCount,
                numberOfResultSuggestions: this._personalResultCount,
                preQuerySuggestions: true,
                hitHighlighting: true,
                showPeopleNameSuggestions: this._showPeopleNameSuggestions,
                capitalizeFirstLetters: this._showPeopleNameSuggestions,
                prefixMatchAllTerms: false
            };

            try {
                // this is where I want to update

                var queryArr = [];
                var personalArr = [];
                var peopleArr;

                jQuery.ajax({
                    url: '_vti_bin/SearchSuggestions.ashx',
                    data: sendInfo,
                    async: true,
                    dataType: "json",
                    success: function(data) {
                        $.each(data.Queries, function() {
                            var suggestion = new Microsoft.SharePoint.Client.Search.Query.QuerySuggestionQuery();
                            suggestion.set_query(this.Query);
                            suggestion.set_isPersonal(this.IsPersonal);
                            queryArr.push(suggestion);
                        });
                        $.each(data.PersonalResults, function() {
                            var personal = new Microsoft.SharePoint.Client.Search.Query.PersonalResultSuggestion();
                            personal.set_highlightedTitle(this.HighlightedTitle);
                            personal.set_isBestBet(this.IsBestBet);
                            personal.set_title(this.Title);
                            personal.set_url(this.Url);
                            personalArr.push(personal);
                        });

                        peopleArr = data.PeopleNames;
                        
                        var newResult = new Microsoft.SharePoint.Client.Search.Query.QuerySuggestionResults();
                        newResult.set_queries(queryArr);
                        newResult.set_peopleNames(peopleArr);
                        newResult.set_personalResults(personalArr);

                        rootCall._update(text, newResult, true);
                    },
                    error: function() {
                        //response( [] );
                    }
                });

            } catch(ex) {
                Srch.U.trace(null, "AutoCompleteBehavior._update", ex.toString());
            }
            //            }
        } catch(e) {
            Srch.U.trace(null, "AutoCompleteBehavior._getSuggestion", e.toString());
        }

        $common.updateFormToRefreshATDeviceBuffer();
    };
}