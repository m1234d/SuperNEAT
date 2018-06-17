using System;
using System.Data;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace SuperNEAT
{
    public class GeneticAlgo
    {
        public static int Inputs = 10; //169 inputs and 1 bias
        public static int Outputs = 9; //8 controller keys
        public static int MaxNodes = 1000000;

        int Population = 300;
        int StaleSpecies = 15;

        double DeltaDisjoint = 2.0;
        double DeltaWeights = 0.4;
        double DeltaThreshold = 1.0;

        double MutateConnectionsChance = 0.25;
        double PerturbChance = 0.90;
        double CrossoverChance = 0.75;
        double LinkMutationChance = 2.0;
        double NodeMutationChance = 0.50;
        double BiasMutationChance = 0.40;
        double StepSize = 0.1;
        double DisableMutationChance = 0.4;
        double EnableMutationChance = 0.2;

        bool xPressed = false;
        bool sPressed = false;
        bool zPressed = false;
        bool aPressed = false;
        bool uPressed = false;
        bool dPressed = false;
        bool lPressed = false;
        bool rPressed = false;

        int ThreadCount = 0;
        public Pool pool;
        Random r = new Random();

        //activation function for each neuron
        double Sigmoid(double x)
        {
            return 2 / (1 + Math.Pow(Math.E, (-4.9 * x))) - 1;
        }

        //returns a unique index for a new connection, used in crossover
        int NewInnovation()
        {
            pool.innovation = pool.innovation + 1;
            return pool.innovation;
        }

        //create a new population pool
        Pool NewPool()
        {

            Pool pool = new Pool(new List<Species>(), 0, Outputs, 0, 0, 0, 0);
            return pool;
        }

        //create a new species
        Species NewSpecies()
        {
            Species species = new Species();
            species.topFitness = 0;
            species.staleness = 0;
            species.genomes = new List<Genome>();
            species.averageFitness = 0;
            return species;
        }

        //create a new genome
        Genome NewGenome()
        {
            Genome genome = new Genome(MutateConnectionsChance, LinkMutationChance, BiasMutationChance, NodeMutationChance, EnableMutationChance, DisableMutationChance, StepSize);
            return genome;
        }

        //return a complete copy of a genome, no references attached
        Genome CopyGenome(Genome genome)
        {
            Genome genome2 = NewGenome();

            for (int g = 0; g < genome.genes.Count; g++)
            {
                genome2.genes.Add(CopyGene(genome.genes[g]));

            }
            genome2.maxneuron = genome.maxneuron;
            genome2.mutationRates.Rows[0]["connections"] = genome.mutationRates.Rows[0]["connections"];

            genome2.mutationRates.Rows[0]["link"] = genome.mutationRates.Rows[0]["link"];

            genome2.mutationRates.Rows[0]["bias"] = genome.mutationRates.Rows[0]["bias"];

            genome2.mutationRates.Rows[0]["node"] = genome.mutationRates.Rows[0]["node"];

            genome2.mutationRates.Rows[0]["enable"] = genome.mutationRates.Rows[0]["enable"];

            genome2.mutationRates.Rows[0]["disable"] = genome.mutationRates.Rows[0]["disable"];


            return genome2;
        }

        //returns a randomly mutated genome
        Genome BasicGenome()
        {
            Genome genome = NewGenome();
            genome.maxneuron = Inputs;
            Mutate(genome);
            return genome;
        }

        //creates a new gene
        Gene NewGene()
        {

            Gene gene = new Gene();
            return gene;
        }

        //returns a copy of a gene
        Gene CopyGene(Gene gene)
        {

            Gene gene2 = NewGene();

            gene2.into = gene.into;
            gene2.outo = gene.outo;
            gene2.weight = gene.weight;
            gene2.enabled = gene.enabled;
            gene2.innovation = gene.innovation;

            return gene2;
        }

        //creates a new neuron(equivalent to a node in the genome structure)
        Neuron NewNeuron()
        {
            Neuron neuron = new Neuron();
            return neuron;
        }

        //creates a neural network based off a genome's connections and neurons
        void GenerateNetwork(Genome genome)
        {
            NeuralNet network = new NeuralNet();
            network.neurons = new List<Neuron>(new Neuron[MaxNodes + Outputs]);
            for (int i = 0; i < MaxNodes + Outputs; i++)
            {
                network.neurons[i] = null;
            }
            for (int i = 0; i < Inputs; i++)
            {
                network.neurons[i] = NewNeuron();
            }
            for (int o = 0; o < Outputs; o++)
            {
                network.neurons[MaxNodes + o] = NewNeuron();
            }

            genome.genes = genome.genes.OrderBy(x => x.outo).ToList();
            for (int i = 0; i < genome.genes.Count; i++)
            {
                Gene gene = genome.genes[i];
                if (gene.enabled)
                {
                    if (network.neurons[gene.outo] == null)
                    {
                        network.neurons[gene.outo] = NewNeuron();
                    }
                    Neuron neuron = network.neurons[gene.outo];
                    neuron.incoming.Add(gene);
                    if (network.neurons[gene.into] == null)
                    {
                        network.neurons[gene.into] = NewNeuron();
                    }
                }
            }
            genome.network = network;
        }

        //returns a list of output controls based off an input list for a network
        List<bool> EvaluateNetwork(NeuralNet network, List<double> inputs)
        {
            inputs.Add(1);
            if (inputs.Count != Inputs)
            {
                Console.WriteLine("Incorrect number of neural network inputs.");
                return new List<bool>();
            }
            for (int i = 0; i < Inputs; i++)
            {
                network.neurons[i].value = inputs[i];
            }
            foreach (Neuron neuron in network.neurons)
            {
                if (neuron == null)
                {
                    continue;
                }
                double sum = 0;
                for (int j = 0; j < neuron.incoming.Count; j++)
                {
                    Gene incoming = neuron.incoming[j];
                    Neuron other = network.neurons[incoming.into];
                    sum += (incoming.weight * other.value);
                }
                if (neuron.incoming.Count > 0)
                {
                    neuron.value = Sigmoid(sum);
                }
            }
            List<bool> outputs = new List<bool>();
            for (int o = 0; o < Outputs; o++)
            {
                if (network.neurons[MaxNodes + o].value > 0)
                {
                    outputs.Add(true);
                }
                else
                {
                    outputs.Add(false);
                }
            }
            return outputs;
        }

        //returns a list of output controls based off an input list for a network
        public List<double> EvaluateNetworkDouble(NeuralNet network, List<double> inputs, int side)
        {
            inputs.Add(side);
            if (inputs.Count != Inputs)
            {
                Console.WriteLine("Incorrect number of neural network inputs.");
                return new List<double>();
            }
            for (int i = 0; i < Inputs; i++)
            {
                network.neurons[i].value = inputs[i];
            }
            foreach (Neuron neuron in network.neurons)
            {
                if (neuron == null)
                {
                    continue;
                }
                double sum = 0;
                for (int j = 0; j < neuron.incoming.Count; j++)
                {
                    Gene incoming = neuron.incoming[j];
                    Neuron other = network.neurons[incoming.into];
                    sum += (incoming.weight * other.value);
                }
                if (neuron.incoming.Count > 0)
                {
                    neuron.value = Sigmoid(sum);
                }
            }
            List<double> outputs = new List<double>();
            for (int o = 0; o < Outputs; o++)
            {
                outputs.Add(network.neurons[MaxNodes + o].value);
            }
            return outputs;
        }

        //returns the highest index of a genome's connections
        int GetHighestInnovation(Genome g)
        {
            int highest = 0;
            for (int i = 0; i < g.genes.Count; i++)
            {
                if (g.genes[i].innovation > highest)
                {
                    highest = g.genes[i].innovation;

                }
            }
            return highest;
        }

        //creates child from excess/disjoint genes of highest fitness genome and matching genes of random genome
        Genome Crossover(Genome ge1, Genome ge2)
        {
            Genome g1;
            Genome g2;
            if (ge2.fitness > ge1.fitness)
            {
                g1 = ge2;
                g2 = ge1;
            }
            else
            {
                g1 = ge1;
                g2 = ge2;
            }
            Genome child = NewGenome();
            List<Gene> innovations2 = new List<Gene>();

            for (int i = 0; i < pool.innovation + 1; i++)
            {
                innovations2.Add(null);
            }
            for (int i = 0; i < g2.genes.Count; i++)
            {
                Gene gene = g2.genes[i];
                innovations2[gene.innovation] = gene;
            }

            for (int i = 0; i < g1.genes.Count; i++)
            {
                Gene gene1 = g1.genes[i];
                Gene gene2 = innovations2[gene1.innovation];
                if (gene2 != null && r.Next(1, 3) == 1 && gene2.enabled)
                {
                    child.genes.Add(CopyGene(gene2));
                }
                else
                {
                    child.genes.Add(CopyGene(gene1));
                }
            }
            child.maxneuron = Math.Max(g1.maxneuron, g2.maxneuron);
            for (int i = 0; i < g1.mutationRates.Columns.Count; i++)
            {
                child.mutationRates.Rows[0][child.mutationRates.Columns[i]] = g1.mutationRates.Rows[0][g1.mutationRates.Columns[i]];
            }
            return child;
        }

        //get index of a random neuron in a genome
        int RandomNeuron(List<Gene> genes, bool nonInput)
        {
            bool[] neurons = new bool[MaxNodes + Outputs];
            if (nonInput == false)
            {
                for (int i = 0; i < Inputs; i++)
                {
                    neurons[i] = true;
                }
            }
            for (int i = 0; i < Outputs; i++)
            {
                neurons[MaxNodes + i] = true;
            }
            for (int i = 0; i < genes.Count; i++)
            {
                if (nonInput == false || genes[i].into >= Inputs)
                {
                    neurons[genes[i].into] = true;
                }
                if (nonInput == false || genes[i].outo >= Inputs)
                {
                    neurons[genes[i].outo] = true;
                }
            }
            int count = 0;
            foreach (bool b in neurons)
            {
                if (b)
                {
                    count++;
                }
            }

            int n = r.Next(0, count + 1);
            for (int i = 0; i < neurons.Length; i++)
            {
                if (neurons[i])
                {
                    n--;
                    if (n == 0)
                    {
                        return i;
                    }
                }
            }

            return 0;
        }

        //checks if a particular gene already exists in a gene set
        bool ContainsLink(List<Gene> genes, Gene link)
        {
            for (int i = 0; i < genes.Count; i++)
            {
                Gene gene = genes[i];
                if (gene.into == link.into && gene.outo == link.outo)
                {
                    return true;
                }
            }
            return false;
        }

        //either slightly perturb or randomly replace each gene in a genome
        void PointMutate(Genome genome)
        {
            double step = Convert.ToDouble(genome.mutationRates.Rows[0]["step"]);
            for (int i = 0; i < genome.genes.Count; i++)
            {
                Gene gene = genome.genes[i];
                if (r.NextDouble() < PerturbChance)
                {
                    gene.weight = gene.weight + r.NextDouble() * (step * 2 - step);
                }
                else
                {
                    gene.weight = r.NextDouble() * 4 - 2;
                }
            }
        }

        //create a new link between unconnected neurons
        void LinkMutate(Genome genome, bool forceBias)
        {
            Gene newLink = NewGene();
            int n1;
            int n2;
            //find two neurons in genome
            while (true)
            {
                n1 = RandomNeuron(genome.genes, false);
                n2 = RandomNeuron(genome.genes, true);
                //make sure not both inputs
                if (n1 < Inputs && n2 < Inputs)
                {
                    continue;
                }
                break;
            }

            int neuron1;
            int neuron2;
            //verify neuron1 is the input
            if (n2 < Inputs)
            {
                neuron1 = n2;
                neuron2 = n1;
            }
            else
            {
                neuron1 = n1;
                neuron2 = n2;
            }
            newLink.into = neuron1;
            newLink.outo = neuron2;
            //if you want to modify the bias connection
            if (forceBias)
            {
                newLink.into = Inputs - 1;
            }
            //make sure link doesnt exist
            if (ContainsLink(genome.genes, newLink))
            {
                return;
            }
            newLink.innovation = NewInnovation();
            newLink.weight = r.NextDouble() * 4 - 2;
            genome.genes.Add(newLink);
        }

        //create a new neuron by splitting a link into two parts
        void NodeMutate(Genome genome)
        {
            if (genome.genes.Count == 0)
            {
                return;
            }
            genome.maxneuron = genome.maxneuron + 1;
            Gene gene = genome.genes[r.Next(0, genome.genes.Count)];
            //cancel if gene isnt enabled
            if (gene.enabled == false)
            {
                return;
            }
            //disable connection, create two new ones
            gene.enabled = false;
            Gene gene1 = CopyGene(gene);
            gene1.outo = genome.maxneuron;
            gene1.weight = 1;
            gene1.innovation = NewInnovation();
            gene1.enabled = true;
            genome.genes.Add(gene1);
            Gene gene2 = CopyGene(gene);
            gene2.into = genome.maxneuron;
            gene2.innovation = NewInnovation();
            gene2.enabled = true;
            genome.genes.Add(gene2);
        }

        //randomly choose a gene to be enabled/disabled
        void EnableDisableMutate(Genome genome, bool enable)
        {
            List<Gene> candidates = new List<Gene>();
            for (int i = 0; i < genome.genes.Count; i++)
            {
                if (genome.genes[i].enabled != enable)
                {
                    candidates.Add(genome.genes[i]);
                }
            }
            if (candidates.Count == 0)
            {
                return;
            }
            Gene gene = candidates[r.Next(0, candidates.Count)];
            gene.enabled = !gene.enabled;
        }

        //run all mutations on a genome
        void Mutate(Genome genome)
        {
            //alter genome's internal mutation rates
            for (int i = 0; i < genome.mutationRates.Columns.Count; i++)
            {
                if (r.Next(0, 2) == 0)
                {
                    genome.mutationRates.Rows[0][genome.mutationRates.Columns[i]] = .95 * Convert.ToDouble(genome.mutationRates.Rows[0][genome.mutationRates.Columns[i]]);
                }
                else
                {
                    genome.mutationRates.Rows[0][genome.mutationRates.Columns[i]] = 1.05263 * Convert.ToDouble(genome.mutationRates.Rows[0][genome.mutationRates.Columns[i]]);
                }
            }
            if (r.NextDouble() < Convert.ToDouble(genome.mutationRates.Rows[0]["connections"]))
            {
                PointMutate(genome);
            }
            double p = Convert.ToDouble(genome.mutationRates.Rows[0]["link"]);
            while (p > 0)
            {
                if (r.NextDouble() < p)
                {
                    LinkMutate(genome, false);
                }
                p = p - 1;

            }
            p = Convert.ToDouble(genome.mutationRates.Rows[0]["bias"]);
            while (p > 0)
            {
                if (r.NextDouble() < p)
                {
                    LinkMutate(genome, true);
                }
                p = p - 1;
            }
            p = Convert.ToDouble(genome.mutationRates.Rows[0]["node"]);
            while (p > 0)
            {
                if (r.NextDouble() < p)
                {
                    NodeMutate(genome);
                }
                p = p - 1;
            }
            p = Convert.ToDouble(genome.mutationRates.Rows[0]["enable"]);
            while (p > 0)
            {
                if (r.NextDouble() < p)
                {
                    EnableDisableMutate(genome, true);
                }
                p = p - 1;
            }
            p = Convert.ToDouble(genome.mutationRates.Rows[0]["disable"]);
            while (p > 0)
            {
                if (r.NextDouble() < p)
                {
                    EnableDisableMutate(genome, false);
                }
                p = p - 1;
            }
        }

        //disjoint part of species distance equation
        int Disjoint(List<Gene> genes1, List<Gene> genes2)
        {
            bool[] i1 = new bool[pool.innovation + 100];
            for (int i = 0; i < genes1.Count; i++)
            {
                Gene gene = genes1[i];
                i1[gene.innovation] = true;
            }
            bool[] i2 = new bool[pool.innovation + 100];
            for (int i = 0; i < genes2.Count; i++)
            {
                Gene gene = genes2[i];
                i2[gene.innovation] = true;
            }
            int disjointGenes = 0;
            for (int i = 0; i < genes1.Count; i++)
            {
                Gene gene = genes1[i];
                if (i2[gene.innovation] == false)
                {
                    disjointGenes = disjointGenes + 1;
                }
            }
            for (int i = 0; i < genes2.Count; i++)
            {
                Gene gene = genes2[i];
                if (i1[gene.innovation] == false)
                {
                    disjointGenes = disjointGenes + 1;
                }
            }
            int n = Math.Max(genes1.Count, genes2.Count);
            if (n == 0)
            {
                n = 1;
            }
            return disjointGenes / n;
        }

        //weight part of species distance equation
        double Weights(List<Gene> genes1, List<Gene> genes2)
        {
            Gene[] i2 = new Gene[pool.innovation + 100];
            for (int i = 0; i < genes2.Count; i++)
            {
                Gene gene = genes2[i];
                i2[gene.innovation] = gene;
            }
            double sum = 0;
            double coincident = 0;
            for (int i = 0; i < genes1.Count; i++)
            {
                Gene gene = genes1[i];
                if (i2[gene.innovation] != null)
                {
                    Gene gene2 = i2[gene.innovation];
                    sum = sum + Math.Abs(gene.weight - gene2.weight);
                    coincident = coincident + 1;
                }
            }
            return (sum / coincident);
        }

        //check if two genomes are part of the same species
        bool SameSpecies(Genome genome1, Genome genome2)
        {
            double dd = DeltaDisjoint * Disjoint(genome1.genes, genome2.genes);
            double dw = DeltaWeights * Weights(genome1.genes, genome2.genes);
            return (dd + dw < DeltaThreshold);
        }

        //assign a global rank to each genome, from lowest fitness to highest
        void RankGlobally()
        {
            List<Genome> global = new List<Genome>();
            for (int s = 0; s < pool.species.Count; s++)
            {
                Species species = pool.species[s];
                for (int g = 0; g < species.genomes.Count; g++)
                {
                    global.Add(species.genomes[g]);
                }
            }
            global = global.OrderBy(x => x.fitness).ToList();
            for (int g = 0; g < global.Count; g++)
            {
                global[g].globalRank = g + 1;
            }
        }

        //calculate the average rank of a species's genomes
        void CalculateAverageFitness(Species species)
        {
            double total = 0;
            for (int g = 0; g < species.genomes.Count; g++)
            {
                Genome genome = species.genomes[g];
                total = total + genome.globalRank;
            }
            species.averageFitness = total / species.genomes.Count;
        }

        //calculate the total of all average fitnesses
        double TotalAverageFitness()
        {
            double total = 0;
            for (int s = 0; s < pool.species.Count; s++)
            {
                Species species = pool.species[s];
                total = total + species.averageFitness;
            }
            return total;
        }

        //remove a certain amount of genomes in every species
        void CullSpecies(bool cutToOne)
        {
            for (int s = 0; s < pool.species.Count; s++)
            {
                Species species = pool.species[s];
                species.genomes = species.genomes.OrderByDescending(x => x.fitness).ToList();
                int remaining = (int)Math.Ceiling((double)species.genomes.Count / (double)2);
                if (cutToOne)
                {
                    remaining = 1;
                }
                while (species.genomes.Count > remaining)
                {
                    species.genomes.RemoveAt(species.genomes.Count - 1);
                }
            }
        }

        //create a new genome from a species
        Genome BreedChild(Species species)
        {
            Genome child;
            if (r.NextDouble() < CrossoverChance)
            {
                Genome g1 = species.genomes[r.Next(0, species.genomes.Count)];
                Genome g2 = species.genomes[r.Next(0, species.genomes.Count)];
                child = Crossover(g1, g2);
            }
            else
            {
                Genome g = species.genomes[r.Next(0, species.genomes.Count)];
                child = CopyGenome(g);
            }
            Mutate(child);
            return child;
        }

        //remove species that havent been the best lately
        void RemoveStaleSpecies()
        {
            List<Species> survived = new List<Species>();
            for (int s = 0; s < pool.species.Count; s++)
            {
                Species species = pool.species[s];
                species.genomes = species.genomes.OrderByDescending(x => x.fitness).ToList();
                if (species.genomes[0].fitness > species.topFitness)
                {
                    species.topFitness = species.genomes[0].fitness;
                    species.staleness = 0;
                }
                else
                {
                    species.staleness = species.staleness + 1;
                }
                if (species.staleness < StaleSpecies || species.topFitness >= pool.maxFitness)
                {
                    survived.Add(species);
                }
            }
            pool.species = survived;
        }

        //remove species that are terrible in global ranking 
        void RemoveWeakSpecies()
        {
            List<Species> survived = new List<Species>();
            double sum = TotalAverageFitness();
            for (int s = 0; s < pool.species.Count; s++)
            {
                Species species = pool.species[s];
                double breed = Math.Floor(species.averageFitness / sum * Population);
                if (breed >= 1)
                {
                    survived.Add(species);
                }
            }
            pool.species = survived;
        }

        //determine a species to add a genome to
        void AddToSpecies(Genome child)
        {
            bool foundSpecies = false;
            for (int s = 0; s < pool.species.Count; s++)
            {
                Species species = pool.species[s];
                if (foundSpecies == false && SameSpecies(child, species.genomes[0]))
                {
                    species.genomes.Add(child);
                    foundSpecies = true;
                }
            }
            if (foundSpecies == false)
            {
                Species childSpecies = NewSpecies();
                childSpecies.genomes.Add(child);
                pool.species.Add(childSpecies);
            }
        }

        //create a new generation of genomes
        void NewGeneration()
        {
            //cuts each species's population in half
            CullSpecies(false);
            RankGlobally();
            RemoveStaleSpecies();
            RankGlobally();
            for (int s = 0; s < pool.species.Count; s++)
            {
                Species species = pool.species[s];
                CalculateAverageFitness(species);
            }
            RemoveWeakSpecies();
            double sum = TotalAverageFitness();
            //breed the species a certain amount of times proportional to their worth
            List<Genome> children = new List<Genome>();
            for (int s = 0; s < pool.species.Count; s++)
            {
                Species species = pool.species[s];
                double breed = Math.Floor(species.averageFitness / sum * Population) - 1;
                for (int i = 0; i < breed; i++)
                {
                    children.Add(BreedChild(species));
                }
            }
            //eliminate all but the best members of each species
            CullSpecies(true);
            //breed the top members until population is full again
            while (children.Count + pool.species.Count < Population)
            {
                Species species = pool.species[r.Next(0, pool.species.Count)];
                children.Add(BreedChild(species));
            }
            //add all the new children to the species
            for (int c = 0; c < children.Count; c++)
            {
                Genome child = children[c];
                AddToSpecies(child);
            }
            pool.generation = pool.generation + 1;

        }

        //create and fill a new pool with random genomes
        void InitializePool()
        {
            pool = NewPool();
            for (int i = 0; i < Population; i++)
            {
                Genome basic = BasicGenome();
                AddToSpecies(basic);
            }
            InitializeRun();
        }

        //generate a network for the current genome
        void InitializeRun()
        {
            Species species = pool.species[pool.currentSpecies];
            Genome genome = species.genomes[pool.currentGenome];
            GenerateNetwork(genome);
        }
        //evaluate the fitness of the current genome
        void EvaluateCurrent(object d)
        {
            int[] data = (int[])d;
            int sp = (int)data[0];
            int g = (int)data[1];
            EvaluateMario();
            Console.WriteLine("Evaluated");
            ThreadCount++;
        }

        //determine the fitness of the current genome for mario
        void EvaluateMario()
        {
            Species species = pool.species[pool.currentSpecies];
            Genome genome = species.genomes[pool.currentGenome];
            GenerateNetwork(genome);

            //gui controls
            Form1.BestGenome = pool.currentGenome;
            Form1.BestFitness = pool.maxFitness;
            Form1.SpeciesCount = pool.currentSpecies;
            Form1.SpeciesTot = pool.species.Count;
            Form1.CurrentGen = pool.generation;
            Form1.OverrideBest(genome);

            double fitness = 0;
            while (Form1.alive == false)
            {

            }
            while (Form1.alive)
            {
                try
                {
                    Form1.ActivateApp("EmuHawk");
                    int[][] inputt = new int[Form1.inputs.Length][];
                    List<double> inputs = new List<double>();
                    Array.Copy(Form1.inputs, inputt, Form1.inputs.Length);
                    for (int i = 0; i < inputt.Length; i++)
                    {
                        for (int p = 0; p < inputt[0].Length; p++)
                        {
                            inputs.Add(inputt[i][p]);
                        }
                    }
                    //get buttons to press
                    List<bool> output = EvaluateNetwork(genome.network, inputs);
                    if(fitness == 0 && output[7] == false)
                    {
                        Form1.alive = false;
                        continue;
                    }
                    string str = "";
                    if (output[0] == true)
                    {
                        str += "x";
                    }
                    if (output[1] == true)
                    {
                        str += "s";
                    }
                    if (output[2] == true)
                    {
                        str += "a";
                    }
                    if (output[3] == true)
                    {
                        str += "z";
                    }
                    if (output[4] == true)
                    {
                        str += "u";
                    }
                    if (output[5] == true)
                    {
                        str += "d";
                    }
                    if (output[6] == true)
                    {
                        str += "l";
                    }
                    if (output[7] == true)
                    {
                        str += "r";
                    }
                    Form1.InputString = str;
                    if (output[0] == true && xPressed == false)
                    {
                        Form1.SendI(Form1.VirtualKeyShort.KEY_X, Form1.ScanCodeShort.KEY_X);
                        xPressed = true;
                    }
                    else if (xPressed)
                    {
                        Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_X, Form1.ScanCodeShort.KEY_X);
                        xPressed = false;
                    }
                    if (output[1] == true && sPressed == false)
                    {
                        Form1.SendI(Form1.VirtualKeyShort.KEY_S, Form1.ScanCodeShort.KEY_S);
                        sPressed = true;
                    }
                    else if (sPressed)
                    {
                        Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_S, Form1.ScanCodeShort.KEY_S);
                        sPressed = false;
                    }
                    if (output[2] == true && aPressed == false)
                    {
                        Form1.SendI(Form1.VirtualKeyShort.KEY_A, Form1.ScanCodeShort.KEY_A);
                        aPressed = true;
                    }
                    else if (aPressed)
                    {
                        Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_A, Form1.ScanCodeShort.KEY_A);
                        aPressed = false;
                    }
                    if (output[3] == true && zPressed == false)
                    {
                        Form1.SendI(Form1.VirtualKeyShort.KEY_Z, Form1.ScanCodeShort.KEY_Z);
                        zPressed = true;
                    }
                    else if (zPressed)
                    {
                        Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_Z, Form1.ScanCodeShort.KEY_Z);
                        zPressed = false;
                    }
                    if (output[4] == true && uPressed == false)
                    {
                        Form1.SendI(Form1.VirtualKeyShort.KEY_H, Form1.ScanCodeShort.KEY_H);
                        uPressed = true;
                    }
                    else if (output[4] == false && uPressed)
                    {
                        Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_H, Form1.ScanCodeShort.KEY_H);
                        uPressed = false;
                    }
                    if (output[5] == true && dPressed == false)
                    {
                        Form1.SendI(Form1.VirtualKeyShort.KEY_B, Form1.ScanCodeShort.KEY_B);
                        dPressed = true;
                    }
                    else if (output[5] == false && dPressed)
                    {
                        Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_B, Form1.ScanCodeShort.KEY_B);
                        dPressed = false;
                    }
                    if (output[6] == true && lPressed == false)
                    {
                        Form1.SendI(Form1.VirtualKeyShort.KEY_V, Form1.ScanCodeShort.KEY_V);
                        lPressed = true;
                    }
                    else if (output[6] == false && lPressed)
                    {
                        Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_V, Form1.ScanCodeShort.KEY_V);
                        lPressed = false;
                    }
                    if (output[7] == true && rPressed == false)
                    {
                        Form1.SendI(Form1.VirtualKeyShort.KEY_N, Form1.ScanCodeShort.KEY_N);
                        rPressed = true;
                    }
                    else if (output[7] == false && rPressed)
                    {
                        Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_N, Form1.ScanCodeShort.KEY_N);
                        rPressed = false;
                    }
                }
                catch (Exception e)
                {
                    continue;
                }
            }
            if (xPressed)
            {
                Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_X, Form1.ScanCodeShort.KEY_X);
                xPressed = false;
            }
            if (sPressed)
            {
                Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_S, Form1.ScanCodeShort.KEY_S);
                sPressed = false;
            }
            if (aPressed)
            {
                Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_A, Form1.ScanCodeShort.KEY_A);
                aPressed = false;
            }
            if (zPressed)
            {
                Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_Z, Form1.ScanCodeShort.KEY_Z);
                zPressed = false;
            }
            if (uPressed)
            {
                Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_H, Form1.ScanCodeShort.KEY_H);
                uPressed = false;
            }
            if (dPressed)
            {
                Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_B, Form1.ScanCodeShort.KEY_B);
                dPressed = false;
            }
            if (lPressed)
            {
                Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_V, Form1.ScanCodeShort.KEY_V);
                lPressed = false;
            }
            if (rPressed)
            {
                Form1.ReleaseKey(Form1.VirtualKeyShort.KEY_N, Form1.ScanCodeShort.KEY_N);
                rPressed = false;
            }

            fitness = Form1.marioX;
            if (fitness <= 0)
            {
                fitness = -1;
            }
            Console.WriteLine("Fitness:" + fitness);
            genome.fitness = fitness;
            if (genome.fitness > pool.maxFitness)
            {
                pool.maxFitness = genome.fitness;
            }
        }

        //cycle through all the genomes
        void NextGenome()
        {
            pool.currentGenome = pool.currentGenome + 1;
            if (pool.currentGenome >= pool.species[pool.currentSpecies].genomes.Count)
            {
                pool.currentGenome = 0;
                pool.currentSpecies = pool.currentSpecies + 1;
                if (pool.currentSpecies >= pool.species.Count)
                {
                    ThreadCount = 0;
                    NewGeneration();
                    pool.best = null;
                    pool.maxFitness = 0;
                    pool.currentSpecies = 0;
                }
            }
        }

        //check if a genome's fitness has already been measured
        bool FitnessAlreadyMeasured()
        {
            Species species = pool.species[pool.currentSpecies];
            Genome genome = species.genomes[pool.currentGenome];

            return (genome.fitness != 0);
        }

        //main training loop
        public void Run()
        {
            InitializePool();   
            while (true)
            {
                //cycle through genomes until unmeasured one is found
                while (FitnessAlreadyMeasured())
                {
                    NextGenome();
                }
                //select genome and species
                Species species = pool.species[pool.currentSpecies];
                Genome genome = species.genomes[pool.currentGenome];
                //evaluate
                int[] tt = new int[] { pool.currentSpecies, pool.currentGenome };
                EvaluateCurrent(tt);

            }
        }
    }
}