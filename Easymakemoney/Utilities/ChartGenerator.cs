using System;
using System.Collections.Generic;
using System.Globalization;
using Microcharts;
using SkiaSharp;

namespace Easymakemoney.Utilities
{
    public static class ChartGenerator
    {
        /// <param name="isMoney">Faux pour des quantités (commandes, articles) : la valeur n'est pas affichée en monnaie.</param>
        public static Chart GenerateChart(List<IStatistiqueData>? dataList, bool isMoney = true)
        {
            var entries = new List<ChartEntry>();
            dataList ??= new List<IStatistiqueData>();
            int totalItems = dataList.Count;

            for (int i = 0; i < totalItems; i++)
            {
                var data = dataList[i];
                SKColor color = GenerateColor(i, totalItems);

                string label = DateTime.TryParseExact(data.Data1, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                    ? date.ToString("dd/MM")
                    : data.Data1;

                entries.Add(new ChartEntry(Convert.ToSingle(data.Data2))
                {
                    Label = label,
                    ValueLabel = isMoney
                        ? SiteCurrency.FormatCents(data.Data2, CultureInfo.CurrentCulture)
                        : data.Data2.ToString("0", CultureInfo.CurrentCulture),
                    Color = color
                });
            }

            return new PointChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent,
                LabelTextSize = 24,
                PointSize = 15
            };
        }

        private static SKColor GenerateColor(int index, int totalItems)
        {
            float hue = (360f / totalItems) * index;
            return SKColor.FromHsv(hue, 70, 90);
        }
    }
}
