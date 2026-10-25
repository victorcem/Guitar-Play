using GuitarPlay.Music;

namespace GuitarPlay.Gameplay;

public enum HitGrade { Perfect, Good, Miss }

/// <summary>
/// O que o jogador tocou. Pelo teclado só sabemos a corda; pela guitarra (Etapa 2)
/// saberemos a nota (MIDI). Campos nulos não são conferidos.
/// </summary>
public readonly record struct PlayedNote(int? String, int? Midi)
{
    public bool Matches(ChartNote note) =>
        (String is null || String == note.String) && (Midi is null || Midi == note.Midi);
}

public sealed record Judgement(int NoteIndex, HitGrade Grade);

/// <summary>Confere o tempo de cada nota e calcula pontos, combo, precisão e estrelas.</summary>
public sealed class Judge
{
    public const double PerfectWindow = 0.06;
    public const double HitWindow = 0.13;

    readonly IReadOnlyList<ChartNote> _notes;
    readonly double[] _times;
    readonly HitGrade?[] _grades;
    int _firstPending;

    public Judge(IReadOnlyList<ChartNote> notes, double[] times)
    {
        _notes = notes;
        _times = times;
        _grades = new HitGrade?[notes.Count];
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
    public int Stars => Accuracy >= 0.95 ? 3 : Accuracy >= 0.80 ? 2 : Accuracy >= 0.60 ? 1 : 0;

    /// <summary>Índice da próxima nota ainda não avaliada, ou -1 se acabou.</summary>
    public int NextPending => _firstPending < Total ? _firstPending : -1;

    double Points => Perfects + 0.75 * Goods;

    public HitGrade? GradeOf(int index) => _grades[index];

    /// <summary>Avalia uma nota tocada. Devolve null se não havia nota para ela (quebra o combo).</summary>
    public Judgement? Hit(PlayedNote played, double now)
    {
        int best = -1;
        double bestOffset = double.MaxValue;
        for (int i = _firstPending; i < Total && _times[i] <= now + HitWindow; i++)
        {
            if (_grades[i] is not null || !played.Matches(_notes[i])) continue;
            double offset = Math.Abs(now - _times[i]);
            if (offset <= HitWindow && offset < bestOffset)
            {
                best = i;
                bestOffset = offset;
            }
        }

        if (best < 0)
        {
            Combo = 0;
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

    /// <summary>Marca como erro as notas que passaram da janela sem ser tocadas.</summary>
    public List<Judgement> SweepMisses(double now)
    {
        var missed = new List<Judgement>();
        for (int i = _firstPending; i < Total && _times[i] < now - HitWindow; i++)
        {
            if (_grades[i] is not null) continue;
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
