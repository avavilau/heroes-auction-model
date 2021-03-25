using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using FinMath.Integration;
using FinMath;
using FinMath.Statistics;
using FinMath.Statistics.Distributions;
using FinMath.LinearAlgebra;
using FinMath.NumericalOptimization.Unconstrained;
//using FinMath.NumericalOptimization.Constrained;

namespace HeroesModel
{

	class Program
	{
		const double TOTAL_MONEY = 1.0;
		const double DEFAULT_MONEY = 0.0;
		const double EPS = 1e-8;
		//const double EPS = 1e-6;
		const int TOWN_TYPES = 10;
		const int TOWN_PARAMS = 2;
		const int ALL_TOWN_PARMAS = TOWN_TYPES * TOWN_PARAMS;
		const int TOTAL_PARAMS = ALL_TOWN_PARMAS;

		public enum Town
		{
			Necropolis = TOWN_PARAMS * 0,
			Dungeon = TOWN_PARAMS * 1,
			Conflux = TOWN_PARAMS * 2,
			Castle = TOWN_PARAMS * 3,
			Tower = TOWN_PARAMS * 4,
			Rampart = TOWN_PARAMS * 5,
			Fortress = TOWN_PARAMS * 6,
			Stronghold = TOWN_PARAMS * 7,
			Cove = TOWN_PARAMS * 8,
			Inferno = TOWN_PARAMS * 9,
		}

		public enum TownShort
		{
			necr = TOWN_PARAMS * 0,
			dung = TOWN_PARAMS * 1,
			flux = TOWN_PARAMS * 2,
			cstl = TOWN_PARAMS * 3,
			towr = TOWN_PARAMS * 4,
			rmpt = TOWN_PARAMS * 5,
			frts = TOWN_PARAMS * 6,
			strd = TOWN_PARAMS * 7,
			cove = TOWN_PARAMS * 8,
			infn = TOWN_PARAMS * 9,
		}


		public struct Sample
		{
			public Town red;
			public Town blue;
			public double redMoney;
			public bool redWin;

			public double blueMoney { get { return -redMoney; } }

			public override string ToString()
			{
				if (redWin)
					return $"[Red_{red}_wins_Blue_{blue}@{Math.Round((redMoney + 1) * 10000)}]";
				else
					return $"[Red_{red}_lose_Blue_{blue}@{Math.Round((redMoney + 1) * 10000)}]";
			}
		}


		static List<Sample> data = new List<Sample>();


		public static double ProbabilityOfRedWin(Vector arg, int redOffset, int blueOffset, double redMoney)
		{
			double a0 = arg[redOffset + 0];
			double b0 = arg[redOffset + 1];

			double a1 = arg[blueOffset + 0];
			double b1 = arg[blueOffset + 1];

			double s = arg[ALL_TOWN_PARMAS];

			double redPower = a0 + b0 * (s + redMoney);
			double bluePower = a1 + b1 * (-redMoney);

			return 1 / (1 + Math.Exp(-(redPower - bluePower)));
		}

		public static double LogLikelyhood(Vector arg)
		{
			double r = 0.0;

			for (int i = 0; i < data.Count; ++i)
			{
				Sample s = data[i];
				if (s.redWin)
					r += Math.Log(ProbabilityOfRedWin(arg, (int)s.red, (int)s.blue, s.redMoney));
				else
					r += Math.Log(1 - ProbabilityOfRedWin(arg, (int)s.red, (int)s.blue, s.redMoney));
			}

			r = -r;

			//int redIndex = 0, blueIndex = 0;
			//foreach (Town red in Enum.GetValues(typeof(Town)))
			//{
			//	blueIndex = 0;
			//	foreach (Town blue in Enum.GetValues(typeof(Town)))
			//	{
			//		double a0 = arg[(int)red + 0];
			//		double b0 = arg[(int)red + 1];

			//		double a1 = arg[(int)blue + 0];
			//		double b1 = arg[(int)blue + 1];
			//		double s = arg[ALL_TOWN_PARMAS];

			//		double redPowerOnLeftEnd = a0 + b0 * (s + -1.0);
			//		double bluePowerOnLeftEnd = a1 + b1 * (1.0);

			//		double redPowerOnRightEnd = a0 + b0 * (s + 1.0);
			//		double bluePowerOnRightEnd = a1 + b1 * (-1.0);

			//		double l1 = arg[ALL_TOWN_PARMAS + 1 + (redIndex * TOWN_TYPES * 4) + (blueIndex * 4) + 0];
			//		double l2 = arg[ALL_TOWN_PARMAS + 1 + (redIndex * TOWN_TYPES * 4) + (blueIndex * 4) + 1];
			//		double c1 = arg[ALL_TOWN_PARMAS + 1 + (redIndex * TOWN_TYPES * 4) + (blueIndex * 4) + 2];
			//		double c2 = arg[ALL_TOWN_PARMAS + 1 + (redIndex * TOWN_TYPES * 4) + (blueIndex * 4) + 3];

			//		r += l1 * (bluePowerOnLeftEnd - redPowerOnLeftEnd + c1* c1);
			//		r += l2 * (bluePowerOnRightEnd - redPowerOnRightEnd + c2 * c2);
			//		++blueIndex;
			//	}
			//	++redIndex;
			//}

			return r;
		}

		public static void PrintData()
		{
			Console.Write($"{"Red\\Blue",-10}");
			foreach (Town blue in Enum.GetValues(typeof(Town)))
				Console.Write($" {blue,-10}");
			Console.WriteLine();


			foreach (Town red in Enum.GetValues(typeof(Town)))
			{
				Console.Write($"{red,-10}");
				foreach (Town blue in Enum.GetValues(typeof(Town)))
				{
					int redWin = (int)data.Sum(x => x.red == red && x.blue == blue && x.redWin ? 1 : 0);
					int blueWin = (int)data.Sum(x => x.red == red && x.blue == blue && !x.redWin ? 1 : 0);

					Console.Write($" {(redWin + "/" + blueWin),-10}");
				}
				Console.WriteLine();
			}
		}

		public static void LogLikelyHoodGradient(Vector arg, Vector gradient)
		{
			Vector copy = arg.Clone();
			double r = LogLikelyhood(arg);
			for (int i = 0; i < copy.Count; ++i)
			{
				copy[i] = arg[i] + EPS;
				double g1 = LogLikelyhood(copy);
				copy[i] = arg[i] - EPS;
				double g2 = LogLikelyhood(copy);

				gradient[i] = (g1 - g2) / (2 * EPS);

				copy[i] = arg[i];
			}
		}

		public static void PrintCoeficients(Vector result)
		{
			foreach (Town t in Enum.GetValues(typeof(Town)))
			{
				double ar = result[(int)t + 0];
				double br = result[(int)t + 1];
				Console.WriteLine($"{t,-10} {ar.ToString("F8"),10} {br.ToString("F8"),10}");
			}

			int redIndex = 0, blueIndex = 0;
			foreach (Town red in Enum.GetValues(typeof(Town)))
			{
				blueIndex = 0;
				foreach (Town blue in Enum.GetValues(typeof(Town)))
				{
					double a0 = result[(int)red + 0];
					double b0 = result[(int)red + 1];

					double a1 = result[(int)blue + 0];
					double b1 = result[(int)blue + 1];

					double s = result[ALL_TOWN_PARMAS];

					double redPowerOnLeftEnd = a0 + b0 * (s + -1.0);
					double bluePowerOnLeftEnd = a1 + b1 * (1.0);

					double redPowerOnRightEnd = a0 + b0 * (s + 1.0);
					double bluePowerOnRightEnd = a1 + b1 * (-1.0);


					Console.ForegroundColor = bluePowerOnLeftEnd < redPowerOnLeftEnd  ? ConsoleColor.Green : ConsoleColor.Red;
					Console.WriteLine($"{red} vs {blue} bluePowerOnLeftEnd < redPowerOnLeftEnd = {bluePowerOnLeftEnd - redPowerOnLeftEnd}");
					Console.ForegroundColor = bluePowerOnRightEnd < redPowerOnRightEnd ? ConsoleColor.Green : ConsoleColor.Red;
					Console.WriteLine($"{red} vs {blue}  bluePowerOnRightEnd < redPowerOnRightEnd = {bluePowerOnRightEnd - redPowerOnRightEnd}");
				}
			}

			Console.ForegroundColor = ConsoleColor.White;


		}

		public static void PrintProbabilities(Vector result, double redMoney)
		{
			Console.Write($"{"Red\\Blue",-10}");
			foreach (Town blue in Enum.GetValues(typeof(Town)))
				Console.Write($" {blue,-10}");
			Console.WriteLine();


			foreach (Town red in Enum.GetValues(typeof(Town)))
			{
				Console.Write($"{red,-10}");
				foreach (Town blue in Enum.GetValues(typeof(Town)))
				{
					double p1 = ProbabilityOfRedWin(result, (int)red, (int)blue, redMoney);
					double p2 = ProbabilityOfRedWin(result, (int)blue, (int)red, -redMoney);

					if (p1 > p2)
						Console.ForegroundColor = ConsoleColor.Green;
					else
						Console.ForegroundColor = ConsoleColor.Red;

					Console.Write($" {ProbabilityOfRedWin(result, (int)red, (int)blue, redMoney).ToString("F8"),-10}");

					Console.ForegroundColor = ConsoleColor.White;

				}
				Console.WriteLine();
			}
		}

		public static void ReadData(string file)
		{
			List<Sample> edges = new List<Sample>();
			using (StreamReader reader = new StreamReader(file))
			{
				reader.ReadLine();

				while (!reader.EndOfStream)
				{
					String[] line = reader.ReadLine().Split(", ");
					if (line.Length != 4)
						continue;
					Sample s;
					s.red = (Town)Enum.Parse(typeof(Town), line[0]);
					s.blue = (Town)Enum.Parse(typeof(Town), line[1]);
					s.redMoney = Double.Parse(line[2]) / 10000;
					s.redWin = Boolean.Parse(line[3]);
					edges.Add(s);
					if (!s.redWin)
					{
						Town t = s.red;
						s.red = s.blue;
						s.blue = t;
						s.redWin = true;
						s.redMoney = -s.redMoney;
						edges.Add(s);
					}
				}
			}


			// Transitive  closure 
			for (int i = 0; i < edges.Count; ++i)
			{
				Sample s = edges[i];
				if (s.redWin)
				{
					TransitiveClosure(edges, s, (o, e) => o.red == e.red && e.redMoney >= o.redMoney && e.blueMoney <= o.redMoney, (o, e) => new Sample() { red = e.blue, blue = o.blue, redMoney = o.redMoney, redWin = true });
					TransitiveClosure(edges, s, (o, e) => o.blue == e.blue && e.blueMoney <= o.blueMoney && e.redMoney >= o.blueMoney, (o, e) => new Sample() { red = o.red, blue = e.red, redMoney = o.redMoney, redWin = true });
				}
				else
				{
					TransitiveClosure(edges, s, (o, e) => o.red == e.blue && e.blueMoney >= o.redMoney, (o, e) => new Sample() { red = e.red, blue = o.blue, redMoney = o.redMoney, redWin = false });
					TransitiveClosure(edges, s, (o, e) => o.blue == e.red && e.redMoney >= o.blueMoney, (o, e) => new Sample() { red = o.red, blue = e.blue, redMoney = o.redMoney, redWin = false });
				}
			}


			//		while (queue.Count > 0)
			//	{
			//		ValueTuple<Town, double> n = queue.Dequeue();
			//		for (int j = 0; j < edges.Count; ++j)
			//		{
			//			Sample e = edges[j];

			//			if (e.red == n.Item1 && e.redWin && e.redMoney <= n.Item2)
			//			{
			//				Sample s = new Sample() { red = e.red }
			//			}
			//			if (e.blue == n.Item1 && e.redWin && e.redMoney <= n.Item2)
			//			{

			//			}
			//		}

			//	}
			//}

			foreach (Town red in Enum.GetValues(typeof(Town)))
			{
				Sample s;
				foreach (Town blue in Enum.GetValues(typeof(Town)))
				{
					s.red = red;
					s.blue = blue;
					s.redWin = false;
					s.redMoney = -1;
					data.Add(s);
					s.redWin = true;
					s.redMoney = 1;
					data.Add(s);

					s.red = red;
					s.blue = blue;
					s.redWin = true;
					s.redMoney = 0;
					data.Add(s);
					s.redWin = false;
					s.redMoney = 0;
					data.Add(s);
				}
			}
		}


		static void TransitiveClosure(List<Sample> edges, Sample s, Func<Sample, Sample, bool> match, Func<Sample, Sample, Sample> merge)
		{
			Queue<Sample> queue = new Queue<Sample>();
			HashSet<Sample> visit = new HashSet<Sample>();
			queue.Enqueue(s);
			visit.Add(s);
			data.Add(s);

			while (queue.Count > 0)
			{
				Sample o = queue.Dequeue();
				foreach (Sample e in edges)
				{
					Sample next = merge(o, e);
					if (!e.redWin && match(o, e) && !visit.Contains(next))
					{
						Console.WriteLine($"Combining {o} with {e} => {next}");

						data.Add(next);
						visit.Add(next);
						queue.Enqueue(next);
					}
				}
			}
		}

		//static void TransitiveClosure(Sample s, List<Sample> edges, Func<Sample, (Town, double), bool> match, Func<Sample, Town> distNode, Func<(Town, double), Sample> nextEdge)
		//{
		//	Queue<(Town, double)> queue = new Queue<(Town, double)>();
		//	Queue<Sample> queue2 = new Queue<Sample>();
		//	HashSet<(Town, double)> visit = new HashSet<(Town, double)>();
		//	(Town, double) startNode = (distNode(s), s.redMoney);
		//	queue.Enqueue(startNode);
		//	queue2.Enqueue(s);
		//	visit.Add(startNode);
		//	data.Add(s);

		//	while (queue.Count > 0)
		//	{
		//		(Town, double) o = queue.Dequeue();
		//		Sample o2 = queue2.Dequeue();
		//		foreach (Sample e in edges)
		//		{
		//			(Town, double) c = (distNode(e), -e.redMoney);
		//			if (!e.redWin && match(e, o) && e.redMoney >= -o.Item2 && !visit.Contains(c))
		//			{
		//				Sample next = nextEdge(c);
		//				data.Add(next);
		//				visit.Add(c);
		//				queue.Enqueue(c);
		//				queue2.Enqueue(next);

		//				Console.WriteLine($"Combining {o2} with {e} => {next}");
		//			}
		//		}
		//	}
		//}



		//public static Boolean SimulateGame(RandomGenerator random, Vector arg, int redOffset, int blueOffset, double redMoney)
		//{
		//	double p = ProbabilityOfRedWin(arg, redOffset, blueOffset, redMoney);

		//	return random.Next() < p;
		//}
		//public static void GenerateModelData(RandomGenerator random, int samplesCount)
		//{
		//	Vector m = new Vector(TOTAL_PARAMS);
		//	for (int col = 0; col < 2; ++col)
		//	{
		//		for (int i = 0; i < TOWN_TYPES; ++i)
		//		{
		//			double a = m[ALL_TOWN_PARMAS * col + TOWN_PARAMS * i + 0] = random.NextDouble() - 0.5;
		//			double b = m[ALL_TOWN_PARMAS * col + TOWN_PARAMS * i + 1] = random.NextDouble() - 0.5;
		//		}
		//	}

		//	Console.WriteLine("Print generated parameters");
		//	PrintCoeficients(new Vector(m));

		//	for (int i = 0; i < samplesCount; ++i)
		//	{
		//		Sample s;
		//		s.red = (Town)(random.Next(TOWN_TYPES) * TOWN_PARAMS);
		//		s.blue = (Town)(random.Next(TOWN_TYPES) * TOWN_PARAMS);
		//		s.redMoney = random.NextDouble() * 2.0 - 1.0;
		//		s.redWin = SimulateGame(random, m, (int)s.red, (int)s.blue + ALL_TOWN_PARMAS, s.redMoney);

		//		data.Add(s);
		//	}
		//}

		public static void EstimateInputData(Vector args, String outputFile)
		{
			using (StreamWriter writer = new StreamWriter(outputFile))
			{
				double error = 0;
				writer.WriteLine("let data_ponints = [");
				for (int i = 0; i < data.Count; ++i)
				{
					Sample s = data[i];

					double prob = ProbabilityOfRedWin(args, (int)s.red, (int)s.blue, s.redMoney);
					double resSample = s.redWin ? 1 : 0;
					error += Math.Abs(prob - resSample);

					if (Math.Abs(prob - resSample) > 0.7)
						Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"Red {s.red} vs Blue {s.blue}. Red Money {(int)((s.redMoney + 1) * 10000)}  Winner: {(s.redWin ? "blue" : "red")}. P(red win): {prob}");
					Console.ForegroundColor = ConsoleColor.White;

					writer.WriteLine($"{{red : \'{(TownShort)s.red}\', blue: \'{(TownShort)s.blue}\', redWin: {s.redWin.ToString().ToLower()}, redMoney: {(s.redMoney + 1) * 10000}}},");
				}
				writer.WriteLine("];");
				Console.WriteLine($"Total error: {error} / {data.Count} = {error / data.Count}");
			}
		}

		public static void PrintCoefficients(Vector args, string outputFile)
		{
			using (StreamWriter writer = new StreamWriter(outputFile))
			{
				writer.WriteLine("let coefficients = { ");
				foreach (Town t in Enum.GetValues(typeof(Town)))
				{
					writer.WriteLine($"{((TownShort)t).ToString().ToLower()}_0  : {args[(int)t + 0]},");
					writer.WriteLine($"{((TownShort)t).ToString().ToLower()}_1  : {args[(int)t + 1]},");
				}
				writer.WriteLine("};");
			}
		}

		static void Main(string[] args)
		{

			RandomGenerator generator = new RandomGenerator();
			//RandomGenerator generator = new RandomGenerator(3);
			ReadData(@"d:\Projects\HeroesModel\HeroesModel\data.csv");
			//GenerateModelData(generator, 1000);

			data.Sort((x, y) =>
			{
				if (x.red != y.red)
					return x.red.CompareTo(y.red);
				return x.blue.CompareTo(y.blue);
			});
			PrintData();

			BFGS bfgs = new BFGS(TOTAL_PARAMS + 1 + TOWN_TYPES * TOWN_TYPES * 4, LogLikelyhood, LogLikelyHoodGradient);
			//BFGS bfgs = new BFGS(TOTAL_PARAMS + 1, LogLikelyhood, LogLikelyHoodGradient);
			//bfgs.FunctionTolerance = EPS;

			DateTime startTime = DateTime.Now;
			ManualResetEvent interrupted = new ManualResetEvent(false);
			Thread reportThread = new Thread(() =>
			{
				while (!interrupted.WaitOne(10000))
				{
					Console.WriteLine($"Time elapsed {(DateTime.Now - startTime)}, iterations complete {bfgs.IterationsDone}");
				}
			});
			reportThread.Start();

			//bfgs.Minimize(new Vector(TOTAL_PARAMS, 0.01));
			//RandomGenerator generator = new RandomGenerator(1);
			//RandomGenerator generator = new RandomGenerator(1);
			Normal normal = new Normal(generator, 0, 0.1);
			bfgs.Minimize(normal.Sample(TOTAL_PARAMS + 1 + TOWN_TYPES * TOWN_TYPES * 4));
			//bfgs.Minimize(normal.Sample(TOTAL_PARAMS + 1));

			interrupted.Set();
			reportThread.Join();

			Vector result = bfgs.MinimumPoint;
			double min = -LogLikelyhood(result);
			Console.WriteLine($"Converged. Time elapsed {(DateTime.Now - startTime)}. Iterations: {bfgs.IterationsDone}");


			for (double money = -0.5; money <= 0.5; money += 0.1)
			{
				Console.WriteLine($"Red money {(money + 1) * 10000}");
				PrintProbabilities(result, money);
			}

			PrintCoeficients(result);

			EstimateInputData(result, @"d:\Projects\HeroesModel\display\data.js");

			PrintCoefficients(result, @"d:\Projects\HeroesModel\display\coefficients.js");

			//GaussKronrodSingular integration = new GaussKronrodSingular();
			//integration.Integrate(x => ConditionalDensity(x, 0, 1, 0, 1), Double.NegativeInfinity, Double.PositiveInfinity, 100);
			//Console.WriteLine(integration.Result);
		}
	}
}
