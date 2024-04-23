using static System.Math;

public class Genes {

    const float mutationChance = .2f;
    const float maxMutationAmount = .3f;
    static readonly System.Random prng = new System.Random ();

    public readonly bool isMale;
    public readonly float desirability;
    public readonly float courage;
    public readonly float happiness;
    public readonly float[] values;

    public Genes (float[] values) {
        this.values = values;
        isMale = values[0] < 0.5f;
        desirability = values[1];
        courage = values[2];
        happiness = values[3];
    }

    public static Genes RandomGenes (int num) {
        float[] values = new float[num];
        for (int i = 0; i < num; i++) {
            values[i] = RandomValue ();
        }
        return new Genes (values);
    }

    public static Genes InheritedGenes(float[] mother, float[] father) {
        float[] values = new float[mother.Length];
        
        values[0] = RandomValue();

        for(int i = 1; i < values.Length; i++)
        {
            values[i] = GeneArbitration(mother[i], father[i]);
        }
        return new Genes(values);
    }

    static float GeneArbitration(float mother, float father)
    {
        float gene = (prng.NextDouble() < 0.5) ? mother : father;

        if(prng.NextDouble() < mutationChance)
        {
            float mutateAmount = RandomGaussian() * maxMutationAmount;
            gene += mutateAmount;
        }
        return gene;
    }

    static float RandomValue () {
        return (float) prng.NextDouble ();
    }

    static float RandomGaussian () {
        double u1 = 1 - prng.NextDouble ();
        double u2 = 1 - prng.NextDouble ();
        double randStdNormal = Sqrt (-2 * Log (u1)) * Sin (2 * PI * u2);
        return (float) randStdNormal;
    }
}