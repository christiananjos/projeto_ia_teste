using Catalogo.Domain.Categorias;

namespace Catalogo.UnitTests.Fixtures;

internal sealed class CategoriaBuilder
{
    private string _nome = "Periféricos";
    private string? _descricao = "Teclados, mouses e headsets";
    private bool _ativa = true;
    private DateTime _agora = Relogio.AgoraUtc;

    public CategoriaBuilder ComNome(string nome)
    {
        _nome = nome;
        return this;
    }

    public CategoriaBuilder ComDescricao(string? descricao)
    {
        _descricao = descricao;
        return this;
    }

    public CategoriaBuilder Inativa()
    {
        _ativa = false;
        return this;
    }

    public CategoriaBuilder CriadaEm(DateTime agoraUtc)
    {
        _agora = agoraUtc;
        return this;
    }

    public Categoria Build()
    {
        var categoria = Categoria.Criar(_nome, _descricao, _agora);
        if (!_ativa)
            categoria.Atualizar(_nome, _descricao, ativa: false, _agora);
        return categoria;
    }
}
