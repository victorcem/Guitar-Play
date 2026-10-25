using GuitarPlay.Music;

namespace GuitarPlay.Gameplay;

public enum HitGrade { Perfect, Good, Miss }

/// <summary>
/// O que o jogador tocou. Pelo teclado só sabemos a corda; pela guitarra (Etapa 2)
/// saberemos a nota (MIDI). Campos nulos não são conferidos.
/// </summary>
public readonly record struct PlayedNote(int? String, int? Midi)
{
    // Na guitarra, o bend conta quando a altura chega na nota-alvo. O detector às vezes erra
    // a oitava; por isso uma oitava acima ou abaixo também vale.
    public bool Matches(ChartNote note) =>
        (String is null || String == note.String) && (Midi is null || SamePitch(Midi.Value, note.TargetMidi));

    public static bool SamePitch(int a, int b) => a == b || Math.Abs(a - b) == 12;
}

public sealed record Judgement(int NoteIndex, HitGrade Grade);

/// <summary>Confere o tempo de cada nota e calcula pontos, combo, precisão e estrelas.</summary>
public sealed class Judge
{
    public const double PerfectWindow = 0.06;
    public const double HitWindow = 0.13;
    // O bend leva um tempo para chegar na nota: aceita até 0,6 s depois, sem perder nada nos primeiros 0,3 s.
    public const double BendWindow = 0.6;
    const double BendGrace = 0.3;

    static double LateWindow(ChartNote note) => note.Bend > 0 ? BendWindow : HitWindow;

    /// <summary>Quanto a nota saiu do tempo, já descontando a folga do bend.</summary>
    static double TimingError(ChartNote note, double offset) =>
        note.Bend > 0 && offset > 0 ? Math.Max(0, offset - BendGrace) : Math.Abs(offset);

    readonly IReadOnlyList<ChartNote> _notes;
    readonly double[] _times;
    readonly HitGrade?[] _grades;
    // Erro de tempo da palhetada de cada bend (guitarra), guardado até a altura chegar no alvo.
    readonly double?[] _bendAttacks;
    int _firstPending;

    public Judge(IReadOnlyList<ChartNote> notes, double[] times)
    {
        _notes = notes;
        _times = times;
        _grades = new HitGrade?[notes.Count];
        _bendAttacks = new double?[notes.Count];
    }

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public int Perfects { get; private set; }
    public int Goods { get; private set; }
    public int Misses { get; private set; }

    public int Total => _notes.Count;
    public int Judged => Perfects + Goods + Misses;
    public int Multiplier => 1 + Math.Min(Combo / 10, 3);
    public double Accuracy => Total == 0 ? 0 : Points / Total;
    public double RunningAccuracy => Judged == 0 ? 1 : Points / Judged;
    public int Stars => StarRating.For(Accuracy);

    /// <summary>Índice da próxima nota ainda não avaliada, ou -1 se acabou.</summary>
    public int NextPending => _firstPending < Total ? _firstPending : -1;

    double Points => Perfects + 0.75 * Goods;

    public HitGrade? GradeOf(int index) => _grades[index];

    /// <summary>Avalia uma nota tocada. Devolve null se não havia nota para ela.</summary>
    /// <param name="penalizeStray">Nota sem alvo quebra o combo (no teclado sim; na guitarra não,
    /// porque cordas soando e ruídos podem gerar notas extras).</param>
    public Judgement? Hit(PlayedNote played, double now, bool penalizeStray = true)
    {
        int best = -1;
        double bestOffset = double.MaxValue;
        for (int i = _firstPending; i < Total && _times[i] <= now + HitWindow; i++)
        {
            if (_grades[i] is not null || !played.Matches(_notes[i])) continue;
            double offset = now - _times[i];
            if (offset < -HitWindow || offset > LateWindow(_notes[i])) continue;
            double error = _bendAttacks[i] ?? TimingError(_notes[i], offset);
            if (error < bestOffset)
            {
                best = i;
                bestOffset = error;
            }
        }

        if (best < 0)
        {
            if (!RecordBendAttack(played, now) && penalizeStray) Combo = 0;
            return null;
        }

        var grade = bestOffset <= PerfectWindow ? HitGrade.Perfect : HitGrade.Good;
        _grades[best] = grade;
        if (grade == HitGrade.Perfect) Perfects++; else Goods++;
        Score += (grade == HitGrade.Perfect ? 100 : 60) * Multiplier;
        Combo++;
        MaxCombo = Math.Max(MaxCombo, Combo);
        Advance();
        return new Judgement(best, grade);
    }

    /// <summary>
    /// Na guitarra, o bend começa na nota de partida: o tempo vale pela palhetada, e o acerto
    /// é confirmado quando a altura chegar no alvo.
    /// </summary>
    bool RecordBendAttack(PlayedNote played, double now)
    {
        if (played.Midi is not { } midi) return false;
        for (int i = _firstPending; i < Total && _times[i] <= now + HitWindow; i++)
        {
            var note = _notes[i];
            double offset = now - _times[i];
            if (note.Bend == 0 || _grades[i] is not null || _bendAttacks[i] is not null) continue;
            if (Math.Abs(offset) > HitWindow || !PlayedNote.SamePitch(midi, note.Midi)) continue;
            _bendAttacks[i] = Math.Abs(offset);
            return true;
        }
        return false;
    }

    /// <summary>Marca como erro as notas que passaram da janela sem ser tocadas.</summary>
    public List<Judgement> SweepMisses(double now)
    {
        var missed = new List<Judgement>();
        for (int i = _firstPending; i < Total && _times[i] < now - HitWindow; i++)
        {
            if (_grades[i] is not null || now - _times[i] <= LateWindow(_notes[i])) continue;
            _grades[i] = HitGrade.Miss;
            Misses++;
            Combo = 0;
            missed.Add(new Judgement(i, HitGrade.Miss));
        }
        Advance();
        return missed;
    }

    void Advance()
    {
        while (_firstPending < Total && _grades[_firstPending] is not null) _firstPending++;
    }
}
