using Newtonsoft.Json.Linq;

namespace Easymakemoney.Utilities
{
    /// <summary>
    /// Lot EDIT : un PATCH n'envoie que ce que le gérant a changé. Un texte vide vaut « rien » (null), des deux côtés, pour qu'un champ
    /// jamais rempli et un champ vidé ne passent pas pour un changement.
    /// </summary>
    public static class EditPayload
    {
        /// <summary>Texte saisi : espaces autour retirés ; vide → null.</summary>
        public static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>Les clés de <paramref name="current"/> dont la valeur diffère de <paramref name="initial"/>.</summary>
        public static JObject Changes(JObject initial, JObject current)
        {
            var changes = new JObject();
            foreach (var property in current.Properties())
            {
                var before = Normalize(initial[property.Name]);
                var after = Normalize(property.Value);
                if (!JToken.DeepEquals(before, after))
                    changes[property.Name] = after.DeepClone();
            }
            return changes;
        }

        private static JToken Normalize(JToken? token) => token switch
        {
            null => JValue.CreateNull(),
            { Type: JTokenType.String } when string.IsNullOrWhiteSpace(token.Value<string>()) => JValue.CreateNull(),
            { Type: JTokenType.String } => new JValue(token.Value<string>()!.Trim()),
            _ => token,
        };
    }
}
