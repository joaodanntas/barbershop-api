using BarberShopApi.Data;
using BarberShopApi.DTOs;
using BarberShopApi.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BarberShopApi.Services;

public class AgendamentoService
{
    private readonly AppDbContext _db;

    public AgendamentoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<HorarioDisponivelDto>> GetHorariosDisponiveis(int barbeiroId, int servicoId, DateOnly data)
    {
        // 1. Busca o serviço para saber a duração
        var servico = await _db.Servicos.FindAsync(servicoId);
        if (servico == null || !servico.Ativo)
            return new List<HorarioDisponivelDto>();

        // Horário mínimo permitido = agora + antecedência mínima do serviço
        var horarioMinimoPermitido = TimeHelper.AgoraBrasil().AddMinutes(servico.AntecedenciaMinimaMinutos);

        // 2. Busca a disponibilidade do barbeiro naquele dia da semana
        var diaSemana = data.DayOfWeek;
        var disponibilidade = await _db.Disponibilidades
            .FirstOrDefaultAsync(d => d.BarbeiroId == barbeiroId && d.DiaSemana == diaSemana);

        if (disponibilidade == null)
            return new List<HorarioDisponivelDto>(); // barbeiro não trabalha nesse dia

        // 3. Busca agendamentos já existentes do barbeiro naquele dia (que não estão cancelados)
        var inicioDia = DateTime.SpecifyKind(data.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var fimDia = DateTime.SpecifyKind(data.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Unspecified);

        var agendamentosExistentes = await _db.Agendamentos
            .Where(a => a.BarbeiroId == barbeiroId
                     && a.DataHoraInicio >= inicioDia
                     && a.DataHoraInicio <= fimDia
                     && a.Status != "Cancelado")
            .Select(a => new { a.DataHoraInicio, a.DataHoraFim })
            .ToListAsync();

        // 4. Gera todos os slots possíveis, respeitando a duração do serviço
        var slots = new List<HorarioDisponivelDto>();
        var duracao = TimeSpan.FromMinutes(servico.DuracaoMinutos);

        var horarioAtual = DateTime.SpecifyKind(data.ToDateTime(disponibilidade.HoraInicio), DateTimeKind.Unspecified);
        var horarioFimExpediente = DateTime.SpecifyKind(data.ToDateTime(disponibilidade.HoraFim), DateTimeKind.Unspecified);

        while (horarioAtual.Add(duracao) <= horarioFimExpediente)
        {
            var slotInicio = horarioAtual;
            var slotFim = horarioAtual.Add(duracao);

            bool antesDoPermitido = slotInicio < horarioMinimoPermitido;

            bool temConflito = agendamentosExistentes.Any(a =>
                slotInicio < a.DataHoraFim && slotFim > a.DataHoraInicio);

            bool naPausa = false;
            if (disponibilidade.PausaInicio.HasValue && disponibilidade.PausaFim.HasValue)
            {
                var pausaInicioDt = DateTime.SpecifyKind(data.ToDateTime(disponibilidade.PausaInicio.Value), DateTimeKind.Unspecified);
                var pausaFimDt = DateTime.SpecifyKind(data.ToDateTime(disponibilidade.PausaFim.Value), DateTimeKind.Unspecified);
                naPausa = slotInicio < pausaFimDt && slotFim > pausaInicioDt;
            }

            if (!antesDoPermitido && !temConflito && !naPausa)
            {
                slots.Add(new HorarioDisponivelDto(slotInicio, slotFim));
            }

            horarioAtual = horarioAtual.AddMinutes(30);
        }

        return slots;
    }
}