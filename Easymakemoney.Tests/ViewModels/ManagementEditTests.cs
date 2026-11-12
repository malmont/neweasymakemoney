using System.Collections.ObjectModel;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.UseCase.LisCommandeUseCase;
using Easymakemoney.UseCase.ListFournisseurUseCase;
using Easymakemoney.UseCase.ListFraisDePortUseCase;
using Easymakemoney.UseCase.ListTransporteurUseCase;
using Easymakemoney.UseCase.PeriodResultUseCase;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Lot EDIT côté mobile : modifier un fournisseur et un frais de port ; frais de port d'une période en une lecture (B3 bis).</summary>
[Collection("SiteCurrency")]
public class ManagementEditTests
{
    private sealed class FakeEditService : IManagementEditService
    {
        public List<(int Id, JObject Changes)> SupplierCalls { get; } = new();
        public List<(int CommandeId, JObject Changes)> ShippingCalls { get; } = new();
        public List<int> PeriodCalls { get; } = new();
        public Exception? UpdateException { get; set; }
        public PeriodShipping? Period { get; set; }
        public Exception? PeriodException { get; set; }

        public Task<ListFournisseur?> UpdateFournisseurAsync(int id, JObject changes)
        {
            SupplierCalls.Add((id, changes));
            if (UpdateException != null)
                throw UpdateException;
            return Task.FromResult<ListFournisseur?>(null);
        }

        public Task<FraisDePort?> UpdateFraisDePortAsync(int commandeId, JObject changes)
        {
            ShippingCalls.Add((commandeId, changes));
            if (UpdateException != null)
                throw UpdateException;
            return Task.FromResult<FraisDePort?>(null);
        }

        public Task<PeriodShipping?> GetPeriodShippingAsync(int collectionId)
        {
            PeriodCalls.Add(collectionId);
            if (PeriodException != null)
                throw PeriodException;
            return Task.FromResult(Period);
        }
    }

    private static ApiException Refusal(string code, string message, HttpStatusCode status = HttpStatusCode.Conflict) =>
        new(new ApiError { Code = code, Message = message }, status);

    private static ListFournisseur Supplier() => new()
    {
        id = 2, name = "Distribution Laurentides", adresse = "12 rue du Port", ville = "Laval", pays = "Canada", tel = "450 555-0187",
        purchaseCurrency = null, typeFournisseurId = 1,
    };

    private static (SupplierEditViewModel Vm, FakeEditService Edit, FakeDialogService Dialog) SupplierVm()
    {
        var types = new FakeListFournisseursServices
        {
            Types = new ObservableCollection<TypeFournisseur> { new() { id = 1, name = "Grossiste" }, new() { id = 2, name = "Artisan" } },
        };
        var edit = new FakeEditService();
        var dialog = new FakeDialogService();
        var vm = new SupplierEditViewModel(edit, new GetListTypeFournisseurUseCase(types), dialog) { GoBackAsync = () => Task.CompletedTask };
        return (vm, edit, dialog);
    }

    // ----- utilitaire -----

    [Fact]
    public void EditPayload_GardeSeulementLesChangements_EtTraiteLeVideCommeRien()
    {
        var initial = new JObject { ["name"] = "A", ["tel"] = null, ["ville"] = "", ["pays"] = "Canada" };
        var current = new JObject { ["name"] = "A ", ["tel"] = "", ["ville"] = null, ["pays"] = "France" };

        var changes = EditPayload.Changes(initial, current);

        Assert.Equal(new JObject { ["pays"] = "France" }.ToString(), changes.ToString());
        Assert.Null(EditPayload.Text("   "));
        Assert.Equal("x", EditPayload.Text(" x "));
    }

    // ----- fournisseur -----

    [Fact]
    public async Task Fournisseur_SansChangement_AucunEnvoi()
    {
        var (vm, edit, dialog) = SupplierVm();
        vm.Load(Supplier());
        await vm.LoadTypesAsync();

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(edit.SupplierCalls);
        Assert.Contains("Aucun changement à enregistrer.", dialog.ShownInfos);
        Assert.Equal("Grossiste", vm.SelectedType?.name);
    }

    [Fact]
    public async Task Fournisseur_SeulLeTelephoneChange_SeulLeTelephonePart()
    {
        var (vm, edit, _) = SupplierVm();
        vm.Load(Supplier());
        await vm.LoadTypesAsync();
        var back = 0;
        vm.GoBackAsync = () => { back++; return Task.CompletedTask; };

        vm.Tel = "450 555-0199";
        await vm.SaveCommand.ExecuteAsync(null);

        var call = Assert.Single(edit.SupplierCalls);
        Assert.Equal(2, call.Id);
        Assert.Equal(new JObject { ["tel"] = "450 555-0199" }.ToString(), call.Changes.ToString());
        Assert.Equal(1, back);
    }

    [Fact]
    public async Task Fournisseur_ChampVide_PartEnNull_TypeEtDevise()
    {
        var (vm, edit, _) = SupplierVm();
        vm.Load(Supplier());
        await vm.LoadTypesAsync();

        vm.Adresse = "  ";
        vm.SelectedType = vm.Types.Single(t => t.id == 2);
        vm.SelectedCurrency = "USD";
        await vm.SaveCommand.ExecuteAsync(null);

        var changes = Assert.Single(edit.SupplierCalls).Changes;
        Assert.Equal(JTokenType.Null, changes["adresse"]!.Type);
        Assert.Equal(2, changes["typeFournisseur"]!.Value<int>());
        Assert.Equal("USD", changes["currency"]!.Value<string>());
        Assert.Equal(3, changes.Count);
    }

    [Fact]
    public async Task Fournisseur_NomVide_RefuseSansEnvoi()
    {
        var (vm, edit, _) = SupplierVm();
        vm.Load(Supplier());

        vm.Name = " ";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(edit.SupplierCalls);
        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task Fournisseur_DeviseFigee_MessageDuServeur_DeviseRemise_EcranReste()
    {
        var (vm, edit, dialog) = SupplierVm();
        vm.Load(Supplier());
        var back = 0;
        vm.GoBackAsync = () => { back++; return Task.CompletedTask; };
        edit.UpdateException = Refusal("FOURNISSEUR_CURRENCY_LOCKED", "Ce fournisseur a déjà 3 commandes : sa devise d'achat ne peut plus changer.");

        vm.SelectedCurrency = "EUR";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Ce fournisseur a déjà 3 commandes : sa devise d'achat ne peut plus changer.", vm.ErrorText);
        Assert.Equal(SupplierEditViewModel.SiteCurrencyChoice, vm.SelectedCurrency);
        Assert.Equal(0, back);
        Assert.Empty(dialog.ShownErrors);
    }

    [Fact]
    public void Fournisseur_DeviseHorsListe_AjouteeAuxChoix()
    {
        var (vm, _, _) = SupplierVm();
        var supplier = Supplier();
        supplier.purchaseCurrency = "gbp";

        vm.Load(supplier);

        Assert.Equal("GBP", vm.SelectedCurrency);
        Assert.Contains("GBP", vm.CurrencyChoices);
        Assert.False(vm.Changes().HasValues);
    }

    // ----- frais de port -----

    private static FraisDePortCommandeRow Row(string currency = "CAD") => new()
    {
        Commande = new ListCommand { id = 15, name = "Facture AN-2026-0457", photo = "", date = "2026-11-01", currency = currency },
        Frais = new FraisDePort
        {
            id = 4, name = "Transport", price = 2490, image = "", facture = "F-12", tracknumber = null,
            transporteur = new Transporteur { id = 2, name = "Nationex" }, currency = currency, siteCurrency = "CAD",
            priceConverted = currency == "CAD" ? 2490 : 3362,
        },
    };

    private static (ShippingEditViewModel Vm, FakeEditService Edit, FakeDialogService Dialog) ShippingVm()
    {
        var transporteurs = new FakeListTransporteurService
        {
            TransporteursResult = new ObservableCollection<Transporteur> { new() { id = 2, name = "Nationex" }, new() { id = 3, name = "Purolator" } },
        };
        var edit = new FakeEditService();
        var dialog = new FakeDialogService();
        var vm = new ShippingEditViewModel(edit, new GetListTransporteurUseCase(transporteurs), dialog) { GoBackAsync = () => Task.CompletedTask };
        return (vm, edit, dialog);
    }

    [Fact]
    public async Task FraisDePort_PrixEtTransporteur_EnvoyesEnCentsEtEnObjet()
    {
        var (vm, edit, _) = ShippingVm();
        vm.Load(Row("USD"));
        await vm.LoadTransporteursAsync();

        Assert.Equal("Prix (USD)", vm.PriceLabel);
        vm.PriceText = "30,10";
        vm.SelectedTransporteur = vm.Transporteurs.Single(t => t.id == 3);
        vm.Tracknumber = "NX0045";
        await vm.SaveCommand.ExecuteAsync(null);

        var call = Assert.Single(edit.ShippingCalls);
        Assert.Equal(15, call.CommandeId);
        Assert.Equal(3010, call.Changes["price"]!.Value<long>());
        Assert.Equal(3, call.Changes["transporteur"]!["id"]!.Value<int>());
        Assert.Equal("NX0045", call.Changes["tracknumber"]!.Value<string>());
        Assert.Equal(3, call.Changes.Count);
    }

    [Fact]
    public async Task FraisDePort_PrixIllisible_RefuseSansEnvoi()
    {
        var (vm, edit, _) = ShippingVm();
        vm.Load(Row());

        vm.PriceText = "abc";
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Empty(edit.ShippingCalls);
        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task FraisDePort_PeriodeClose_MessageDuServeurMontre()
    {
        var (vm, edit, dialog) = ShippingVm();
        vm.Load(Row());
        edit.UpdateException = Refusal("PERIOD_CLOSED", "Cette période est clôturée.");

        vm.Facture = "F-13";
        await vm.SaveCommand.ExecuteAsync(null);

        var shown = Assert.IsType<ApiException>(Assert.Single(dialog.ShownErrors));
        Assert.Equal("PERIOD_CLOSED", shown.ApiError.Code);
    }

    [Fact]
    public void Ligne_DeviseEtrangere_MontreLeMontantConvertiParLeServeur()
    {
        SiteCurrency.Reset();
        var foreign = Row("USD");
        var local = Row("CAD");

        Assert.Contains("≈", foreign.PriceText);
        Assert.DoesNotContain("≈", local.PriceText);
        Assert.True(foreign.CanEditFrais);
        Assert.False(new FraisDePortCommandeRow { Commande = foreign.Commande, Frais = foreign.Frais, PeriodClosed = true }.CanEditFrais);
    }

    // ----- liste des frais de port d'une période -----

    private static ListNewFraisDePortViewModel ListVm(FakeListFraisDePortService frais, FakeEditService? edit)
    {
        var commandes = new FakeListCommandService();
        commandes.Commands.Add(new ListCommand { id = 15, name = "A", photo = "", date = "2026-11-01" });
        commandes.Commands.Add(new ListCommand { id = 16, name = "B", photo = "", date = "2026-11-02" });
        return new ListNewFraisDePortViewModel(null!, new GetListCommandUseCase(commandes), new GetListFraisDePortUseCase(frais),
            new DeleteFraisDePortUseCase(frais), null!, new PeriodUseCase(new FakePeriodService()), null!, new FakeDialogService(), edit)
        {
            CollectionId = 8,
        };
    }

    [Fact]
    public async Task ListeFraisDePort_UneSeuleLecturePourLaPeriode()
    {
        var frais = new FakeListFraisDePortService();
        var edit = new FakeEditService
        {
            Period = new PeriodShipping
            {
                commandes = { new PeriodShippingCommande { commandeId = 15, fraisDePort = Row().Frais }, new PeriodShippingCommande { commandeId = 16 } },
            },
        };
        var vm = ListVm(frais, edit);

        await vm.GetListFraisDePort();

        Assert.Equal(new[] { 8 }, edit.PeriodCalls);
        Assert.Empty(frais.GetCalls);
        Assert.Equal(2, vm.Rows.Count);
        Assert.True(vm.Rows[0].HasFrais);
        Assert.False(vm.Rows[1].HasFrais);
    }

    [Fact]
    public async Task ListeFraisDePort_LecturePeriodeIndisponible_RevientAUneLectureParCommande()
    {
        var frais = new FakeListFraisDePortService();
        frais.FraisByCommandId[15] = Row().Frais;
        var edit = new FakeEditService { PeriodException = Refusal("NOT_FOUND", "Not Found", HttpStatusCode.NotFound) };
        var vm = ListVm(frais, edit);

        await vm.GetListFraisDePort();

        Assert.Equal(new[] { 15, 16 }, frais.GetCalls.OrderBy(id => id));
        Assert.True(vm.Rows.Single(r => r.Commande.id == 15).HasFrais);
    }

    [Fact]
    public async Task ListeFraisDePort_Modifier_OuvreLaFicheAvecLaLigne()
    {
        var edit = new FakeEditService { Period = new PeriodShipping { commandes = { new PeriodShippingCommande { commandeId = 15, fraisDePort = Row().Frais } } } };
        var vm = ListVm(new FakeListFraisDePortService(), edit);
        string? route = null;
        IDictionary<string, object>? parameters = null;
        vm.NavigateAsync = (r, p) => { route = r; parameters = p; return Task.CompletedTask; };
        await vm.GetListFraisDePort();

        await vm.EditFraisCommand.ExecuteAsync(vm.Rows[0]);

        Assert.Equal("ShippingEditPage", route);
        Assert.Same(vm.Rows[0], parameters!["Row"]);
    }
}
