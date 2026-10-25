using System;
using System.Collections.Generic;

namespace CreditPulse.Web.Models
{
    /// <summary>Estado de una fila individual dentro de un lote de carga batch.</summary>
    public enum EstadoFilaLote
    {
        Evaluado,
        Error
    }

    public static class EstadoFilaLoteExtensions
    {
        public static string ToDbValue(this EstadoFilaLote estado)
        {
            return estado == EstadoFilaLote.Evaluado ? "Evaluado" : "Error";
        }

        public static EstadoFilaLote FromDbValue(string valor)
        {
            return string.Equals(valor, "Evaluado", StringComparison.OrdinalIgnoreCase)
                ? EstadoFilaLote.Evaluado
                : EstadoFilaLote.Error;
        }
    }

    /// <summary>
    /// Estado del lote completo (ver Database/09_BatchAsincrono.sql). El
    /// archivo se procesa en segundo plano, así que un lote recién creado
    /// queda en Procesando y la pantalla de resultado se refresca sola
    /// hasta que llega a un estado terminal (Completado o Error).
    /// </summary>
    public enum EstadoLote
    {
        Procesando,
        Completado,
        Error
    }

    public static class EstadoLoteExtensions
    {
        public static string ToDbValue(this EstadoLote estado)
        {
            switch (estado)
            {
                case EstadoLote.Procesando: return "Procesando";
                case EstadoLote.Completado: return "Completado";
                case EstadoLote.Error: return "Error";
                default: throw new ArgumentOutOfRangeException(nameof(estado));
            }
        }

        public static EstadoLote FromDbValue(string valor)
        {
            switch ((valor ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "PROCESANDO": return EstadoLote.Procesando;
                case "ERROR": return EstadoLote.Error;
                default: return EstadoLote.Completado;
            }
        }
    }

    /// <summary>Encabezado de una carga de Evaluaciones Batch (un archivo Excel subido).</summary>
    public class Lote
    {
        public int LoteId { get; set; }
        public string NombreArchivo { get; set; }
        public int UsuarioId { get; set; }
        public string AnalistaNombre { get; set; }

        public int TotalRegistros { get; set; }

        /// <summary>Cuántas filas ya se procesaron (avanza mientras Estado == Procesando).</summary>
        public int TotalProcesados { get; set; }

        public int TotalVerde { get; set; }
        public int TotalAmarillo { get; set; }
        public int TotalRojo { get; set; }
        public int TotalErrores { get; set; }

        public EstadoLote Estado { get; set; }

        /// <summary>Solo tiene valor si Estado == Error: motivo por el que el proceso en segundo plano no pudo terminar.</summary>
        public string MensajeError { get; set; }

        public DateTime FechaCreacion { get; set; }

        public List<LoteEvaluacion> Detalle { get; set; } = new List<LoteEvaluacion>();

        /// <summary>Porcentaje 0-100 para la barra de progreso (evita división por cero con TotalRegistros = 0).</summary>
        public int PorcentajeProgreso
        {
            get { return TotalRegistros <= 0 ? 0 : (int)Math.Min(100, Math.Round(TotalProcesados * 100.0 / TotalRegistros)); }
        }
    }

    /// <summary>Una fila del Excel procesada: puede tener una Evaluacion asociada o haber fallado antes de evaluarse.</summary>
    public class LoteEvaluacion
    {
        public int LoteEvaluacionId { get; set; }
        public int LoteId { get; set; }
        public int? EvaluacionId { get; set; }
        public int FilaExcel { get; set; }

        public string NombreCompletoOriginal { get; set; }
        public string NitOriginal { get; set; }
        public string DpiOriginal { get; set; }

        public EstadoFilaLote Estado { get; set; }
        public string MensajeError { get; set; }

        /// <summary>Solo tiene valor cuando Estado == Evaluado.</summary>
        public ResultadoSemaforo? ResultadoGeneral { get; set; }
    }
}
