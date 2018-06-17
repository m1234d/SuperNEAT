using System;
using System.Data;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperNEAT
{
    public class Pool
    {
        public List<Species> species;
        public int generation;
        public int innovation;
        public int currentSpecies;
        public int currentGenome;
        public Genome best;
        public int currentFrame;
        public double maxFitness;
        public Pool(List<Species> sp, int gen, int innov, int currentSp, int currentGen, int currentFr, int maxFit)
        {
            species = sp;
            generation = gen;
            innovation = innov;
            currentSpecies = currentSp;
            currentGenome = currentGen;
            currentFrame = currentFr;
            maxFitness = maxFit;
        }
    }
    public class Species
    {
        public List<Genome> genomes = new List<Genome>();
        public double topFitness = 0;
        public double staleness = 0;
        public double averageFitness = 0;

    }
    public class Genome
    {
        public List<Gene> genes = new List<Gene>();
        public double fitness = 0;
        public double adjustedFitness = 0;
        public NeuralNet network = new NeuralNet();
        public int maxneuron = 0;
        public int globalRank = 0;
        public DataTable mutationRates;
        public Genome(double conChance, double linkChance, double biasChance, double nodeChance, double enableChance, double disableChance, double stepSize)
        {
            mutationRates = new DataTable();
            DataColumn cl = new DataColumn("connections");
            DataColumn c2 = new DataColumn("link");
            DataColumn c3 = new DataColumn("bias");
            DataColumn c4 = new DataColumn("node");
            DataColumn c5 = new DataColumn("enable");
            DataColumn c6 = new DataColumn("disable");
            DataColumn c7 = new DataColumn("step");
            mutationRates.Columns.Add(cl);
            mutationRates.Columns.Add(c2);
            mutationRates.Columns.Add(c3);
            mutationRates.Columns.Add(c4);
            mutationRates.Columns.Add(c5);
            mutationRates.Columns.Add(c6);
            mutationRates.Columns.Add(c7);
            DataRow row = mutationRates.NewRow();
            row["connections"] = conChance;
            row["link"] = linkChance;
            row["bias"] = biasChance;
            row["node"] = nodeChance;
            row["enable"] = enableChance;
            row["disable"] = disableChance;
            row["step"] = stepSize;
            mutationRates.Rows.Add(row);
        }

    }
    public class Gene
    {
        public int into = 0;
        public int outo = 0;
        public double weight = 0.0;
        public bool enabled = true;
        public int innovation = 0;
    }

    public class NeuralNet
    {
        public List<Neuron> neurons = new List<Neuron>();
    }

    public class Neuron
    {
        public List<Gene> incoming = new List<Gene>();
        public Point loc;
        public double value = 0;
    }
}
