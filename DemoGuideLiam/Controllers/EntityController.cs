using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch.Models;
using Microsoft.Rest;

namespace DemoGuideLiam.Controllers
{
    [Route("api/[controller]")]
    public class EntityController : Controller
    {
        // GET api/values
        [HttpGet]
        public string Get(string textToInspect)
        {
            // Create a client.
            ITextAnalyticsAPI client = new TextAnalyticsAPI();
            client.AzureRegion = AzureRegions.Westeurope;
            client.SubscriptionKey = "4e3daf160cc045ccbbd9092b9ce56add";

            StringBuilder sb = new StringBuilder();


            // Extracting language
            sb.AppendLine("===== LANGUAGE EXTRACTION ======");

            LanguageBatchResult result = client.DetectLanguage(
                    new BatchInput(
                        new List<Input>()
                        {
                          new Input("1", textToInspect),
                        }));

            // Printing language results.
            foreach (var document in result.Documents)
            {
                sb.AppendLine($"Document ID: {document.Id} , Language: {document.DetectedLanguages[0].Name}");

                // Getting key-phrases
                sb.AppendLine("\n\n===== KEY-PHRASE EXTRACTION ======");

                var isoLanguageName = document.DetectedLanguages[0].Iso6391Name;

                KeyPhraseBatchResult phraseResult = client.KeyPhrases(
                        new MultiLanguageBatchInput(
                            new List<MultiLanguageInput>()
                            {
                          new MultiLanguageInput(isoLanguageName, "1", textToInspect),
                            }));

                var phrasesFound = phraseResult.Documents.FirstOrDefault();
                if (phrasesFound == null)
                {
                    throw new Exception("Failed processing message - no phrase result");
                }

                sb.AppendLine($"Document ID: {phrasesFound.Id} ");

                sb.AppendLine("\t Key phrases:");

                foreach (string keyphrase in phrasesFound.KeyPhrases)
                {
                    sb.AppendLine("\t\t" + keyphrase);

                    var entitySearchApi= new EntitySearchAPI(new ApiKeyServiceClientCredentials("3b50878b7c2f4f6d8098a245f4212978"));
                    var entityData = entitySearchApi.Entities.Search(keyphrase);
                    if (entityData?.Entities?.Value?.Count > 0)
                    {
                        // find the entity that represents the dominant one
                        var mainEntity = entityData.Entities.Value.Where(thing => thing.EntityPresentationInfo.EntityScenario == EntityScenario.DominantEntity).FirstOrDefault();

                        if (mainEntity != null)
                        {
                            sb.AppendLine($"Searched for {keyphrase} and found a dominant entity with this description:");
                            sb.AppendLine(mainEntity.Description);
                        }
                        else
                        {
                            sb.AppendLine($"Couldn't find a main entity for {keyphrase}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"No data returned for entity {keyphrase}");
                    }

                }

                // Extracting sentiment
                sb.AppendLine("\n\n===== SENTIMENT ANALYSIS ======");

                SentimentBatchResult sentimentResult = client.Sentiment(
                        new MultiLanguageBatchInput(
                            new List<MultiLanguageInput>()
                            {
                          new MultiLanguageInput(isoLanguageName, "0", textToInspect),
                            }));


                var sentiment = sentimentResult.Documents.FirstOrDefault();
                if (sentiment == null)
                {
                    throw new Exception("Failed processing message - no sentiment result");
                }

                // Printing sentiment results
                sb.AppendLine($"Document ID: {sentiment.Id} , Sentiment Score: {sentiment.Score}");

            }

            return sb.ToString();

        }
        
        
    }
}
