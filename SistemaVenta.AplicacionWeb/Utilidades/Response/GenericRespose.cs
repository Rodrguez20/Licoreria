namespace SistemaVenta.AplicacionWeb.Utilidades.Response
{
    public class GenericRespose<TObject>
    {
        public bool Estado { get; set; }
        public string? Mensaje { get; set; }
        public TObject? Objeto { get; set; }
        public List<TObject>? ListaObjeto { get; set; }
    }
}
