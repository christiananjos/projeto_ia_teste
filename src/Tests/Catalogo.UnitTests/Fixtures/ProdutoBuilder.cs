using Catalogo.Domain.Produtos;

namespace Catalogo.UnitTests.Fixtures;

internal sealed class ProdutoBuilder
{
    private Guid _categoriaId = Guid.CreateVersion7();
    private string _nome = "Teclado Mecânico";
    private string _sku = "TEC-001";
    private string? _descricao = "ABNT2";
    private decimal _preco = 349.90m;
    private bool _ativo = true;
    private bool _removido;
    private DateTime _agora = Relogio.AgoraUtc;

    public ProdutoBuilder NaCategoria(Guid categoriaId)
    {
        _categoriaId = categoriaId;
        return this;
    }

    public ProdutoBuilder ComNome(string nome)
    {
        _nome = nome;
        return this;
    }

    public ProdutoBuilder ComSku(string sku)
    {
        _sku = sku;
        return this;
    }

    public ProdutoBuilder ComPreco(decimal preco)
    {
        _preco = preco;
        return this;
    }

    public ProdutoBuilder Inativo()
    {
        _ativo = false;
        return this;
    }

    public ProdutoBuilder Removido()
    {
        _removido = true;
        return this;
    }

    public ProdutoBuilder CriadoEm(DateTime agoraUtc)
    {
        _agora = agoraUtc;
        return this;
    }

    public Produto Build()
    {
        var produto = Produto.Criar(_categoriaId, _nome, Sku.Criar(_sku), _descricao, _preco, _agora);
        if (!_ativo)
            produto.DefinirStatus(false, _agora);
        if (_removido)
            produto.Remover(_agora);
        return produto;
    }
}
