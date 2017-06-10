using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KSP.Localization;

namespace Starstrider42.CustomAsteroids {
	/// <summary>
	/// Represents a persistence-safe collection of words with assigned weights. For persistance purposes, the 
	/// collection can be viewed as a collection of strings, with each string encoding both word and weight.
	/// </summary>
	/// 
	/// <remarks>To be recognized as a collection by ConfigNodes, a class must have a 
	/// type parameter <c>T</c>, implement <c>System.Collections.Generic.IEnumerable<T></c>, 
	/// and implement <c>System.Collections.IList</c>. Quite the odd combination...</remarks>
	/// 
	/// <typeparam name="Dummy">Required for proper ConfigNode handling. Only Proportions&lt;string&rt; will 
	/// work correctly.</typeparam>
	internal class Proportions<Dummy> : System.Collections.IList, IEnumerable<string> {
		/// <summary>Parsed representation of the collection.</summary>
		private readonly List<Pair<string, double>> props;

		/// <summary>
		/// Creates an empty list of proportions.
		/// </summary>
		/// <remarks>Must be public so that it is accessible to [default namespace].ConfigNode.</remarks>
		public Proportions() {
			this.props = new List<Pair<string, double>>();
		}

		/// <summary>
		/// Parses proportions from a list of appropriately formatted strings.
		/// </summary>
		/// 
		/// <param name="buffer">A collection of strings, assumed to be in the format "[weight] [word]".</param>
		/// <exception cref="ArgumentException">Thrown if any element of <c>buffer</c> does not have the correct 
		/// format.</exception>
		internal Proportions(IEnumerable<string> buffer) {
			this.props = new List<Pair<string, double>>();
			foreach (string item in buffer) {
				this.Add(item);
			}
		}

		/// <summary>
		/// Parses a string as a value with a proportion.
		/// </summary>
		/// 
		/// <param name="input">A string, assumed to be in the format "[weight] [word]".</param>
		/// <returns>A pair ([word], [weight])</returns>
		/// 
		/// <exception cref="ArgumentException">Thrown if <c>input</c> does not have the correct 
		/// format. The program state shall be unchanged in the event of an exception.</exception>
		private static Pair<string, double> parse(string input) {
			Regex inputTemplate = new Regex("(?<rate>[-+.e\\d]+)\\s+(?<id>\\w+)", 
									  RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

			if (inputTemplate.Match(input).Groups[0].Success) {
				GroupCollection parsed = inputTemplate.Match(input).Groups;
				double rate;
				if (!Double.TryParse(parsed["rate"].ToString(), out rate)) {
					throw new ArgumentException(
						Localizer.Format ("#autoLOC_CustomAsteroids_ErrorProportionBadRate", parsed["rate"]));
				}

				return new Pair<string, double>(parsed["id"].ToString(), rate);
			} else {
				throw new ArgumentException (
					Localizer.Format ("#autoLOC_CustomAsteroids_ErrorProportionBadFormat", input));
			}
		}

		/// <summary>
		/// Converts a word and weight into a string in the format "[weight] [word]". Does not throw exceptions.
		/// </summary>
		/// 
		/// <param name="innerData">The word and weight to convert.</param>
		/// <returns>A string <c>x</c> such that <c>parse(x) == innerData</c>.</returns>
		private static string unparse(Pair<string, double> innerData) {
			const string OUTPUT_TEMPLATE = "{1} {0}";

			return String.Format(OUTPUT_TEMPLATE, innerData.first, innerData.second);
		}

		/// <summary>Represents this collection as a list of paired words and weights. Updates to the state 
		/// of this object shall be reflected in the returned value. On the other hand, the returned value 
		/// cannot be used to alter the state of this object. This method does not throw exceptions.</summary>
		/// 
		/// <returns>A list of words and weights. The list representation cannot be modified directly.</returns>
		internal IList<Pair<string, double>> asPairList() {
			return props.AsReadOnly();
		}

		public IEnumerator<string> GetEnumerator() {
			return new StringView(props);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public void RemoveAt(int index) {
			props.RemoveAt(index);
		}

		public object this[int index] {
			get {
				return unparse(props[index]);
			}
			set {
				props[index] = parse((string) value);
			}
		}

		public void Clear() {
			props.Clear();
		}

		public int Count {
			get {
				return props.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public int Add(object value) {
			props.Add(parse((string) value));
			return props.Count - 1;
		}

		public bool Contains(object value) {
			return props.Contains(parse((string) value));
		}

		public int IndexOf(object value) {
			return props.IndexOf(parse((string) value));
		}

		public void Insert(int index, object value) {
			props.Insert(index, parse((string) value));
		}

		public void Remove(object value) {
			props.Remove(parse((string) value));
		}

		public bool IsFixedSize {
			get {
				return false;
			}
		}

		public void CopyTo(Array array, int index) {
			Pair<string, double>[] buffer = new Pair<string, double>[array.Length];
			props.CopyTo(buffer, index);
			for (int i = 0; i < array.Length; i++) {
				array.SetValue(unparse(buffer[i]), i);
			}
		}

		public object SyncRoot {
			get {
				return props;
			}
		}

		public bool IsSynchronized {
			get {
				return false;
			}
		}

		/// <summary>
		/// An enumerator that visits each word-weight pair in this object. The enumerator's methods are expressed 
		/// in terms of strings of the form "[weight] [word]", and may be used to traverse the persistent 
		/// representation of this object.
		/// </summary>
		private class StringView : IEnumerator<string> {
			private readonly IEnumerator<Pair<string, double>> baseEnum;

			internal StringView(List<Pair<string, double>> impl) {
				baseEnum = impl.GetEnumerator();
			}

			public bool MoveNext() {
				return baseEnum.MoveNext();
			}

			public void Reset() {
				baseEnum.Reset();
			}

			object System.Collections.IEnumerator.Current {
				get {
					return Current;
				}
			}

			public void Dispose() {
				baseEnum.Dispose();
			}

			public string Current {
				get {
					return unparse(baseEnum.Current);
				}
			}
		}
	}
}
