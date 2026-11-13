using Easymakemoney.Models;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Corrections issues de la relecture à froid R1 (Gemini) : listes de choix, listes de formulaire en échec.</summary>
[Collection("SiteCurrency")]
public class R1CorrectionsTests
{
    private sealed class Item : IListItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Photo { get; init; } = "";
    }

    [Fact]
    public async Task ListeDeChoix_ChoixAsynchrone_EstAttendu()
    {
        var done = false;
        var vm = new BottomSheetPopupListViewViewModel(
            () => Task.FromResult<IEnumerable<IListItem>>(new[] { new Item { Id = 1 } }),
            onItemTappedAction: async item => { await Task.Yield(); done = item.Id == 1; });

        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)vm.ItemTappedCommand).ExecuteAsync(new Item { Id = 1 });

        Assert.True(done);
    }

    [Fact]
    public async Task ListeDeChoix_ChoixQuiEchoue_NeFaitPasPlanter_EtFenetreAbsenteSansErreur()
    {
        var vm = new BottomSheetPopupListViewViewModel(
            () => Task.FromResult<IEnumerable<IListItem>>(Array.Empty<IListItem>()),
            onItemTappedAction: async _ => { await Task.Yield(); throw new InvalidOperationException("panne"); });

        var command = (CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)vm.ItemTappedCommand;
        await command.ExecuteAsync(new Item { Id = 2 });

        Assert.Null(command.ExecutionTask?.Exception);
    }

    [Fact]
    public async Task ListeDeChoix_ChoixSynchrone_Garde()
    {
        IListItem? chosen = null;
        var vm = new BottomSheetPopupListViewViewModel(
            () => Task.FromResult<IEnumerable<IListItem>>(Array.Empty<IListItem>()),
            onItemTappedAction: item => { chosen = item; });

        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)vm.ItemTappedCommand).ExecuteAsync(new Item { Id = 3 });

        Assert.Equal(3, chosen?.Id);
    }

    [Fact]
    public async Task ListeDeFormulaireEnEchec_UnSeulMessagePourPlusieursListes()
    {
        var shown = new List<string>();
        var now = new DateTime(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);
        FormLoadNotice.ResetForTests();
        FormLoadNotice.UtcNow = () => now;
        FormLoadNotice.ShowAsync = (_, message) => { shown.Add(message); return Task.CompletedTask; };
        try
        {
            await FormLoadNotice.ReportAsync("Categories", new Exception("réseau"));
            await FormLoadNotice.ReportAsync("Styles", new Exception("réseau"));
            Assert.Single(shown);
            Assert.Equal(FormLoadNotice.Message, shown[0]);

            now = now.AddSeconds(11);
            await FormLoadNotice.ReportAsync("Categories", new Exception("réseau"));
            Assert.Equal(2, shown.Count);
        }
        finally
        {
            FormLoadNotice.UtcNow = () => DateTime.UtcNow;
            FormLoadNotice.ShowAsync = AppDialogs.InfoAsync;
            FormLoadNotice.ResetForTests();
        }
    }
}
