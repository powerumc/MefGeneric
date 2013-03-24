using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace System.ComponentModel.Composition.Hosting.Extension
{
	public class GenericCatalog : ComposablePartCatalog
	{
		const string STRING_REGEX_GENERIC_TYPE_ALAYSIS = @"[\d\w.`]*(\[[\d\w ,=.]*\])";
		const char BARCKET_OPENING_GENERIC = '[';
		const char BARCKET_CLOSING_GENERIC = ']';
		const char STRING_BARCKET_REPLACING = ' ';
		const char BACK_QUOTE_GENERIC_ARITY = '`';

		private AggregateCatalog catalog = new AggregateCatalog();

		public GenericCatalog(ComposablePartCatalog catalog)
		{
			this.SourceCatalog = catalog;
			this.catalog.Catalogs.Add(SourceCatalog);
		}

		public ComposablePartCatalog SourceCatalog { get; set; }

		private IEnumerable<Type> GetGenericTypeParameters(string typeFullName)
		{
			Regex regex = new Regex(STRING_REGEX_GENERIC_TYPE_ALAYSIS);
			MatchCollection results = regex.Matches(typeFullName);

			foreach (Match result in results)
			{
				if (result.Success)
				{
					string typeName = result.Value.Replace(BARCKET_OPENING_GENERIC, STRING_BARCKET_REPLACING)
													.Replace(BARCKET_CLOSING_GENERIC, STRING_BARCKET_REPLACING);

					yield return Type.GetType(typeName);
				}
			}
		}



		public override IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> GetExports(ImportDefinition definition)
		{
			int genericIndex = definition.ContractName.IndexOf(BARCKET_OPENING_GENERIC);
			if (genericIndex >= 0)
			{
				string genericDefinitionName = definition.ContractName.Substring(0, genericIndex);
				var genericTypes = GetGenericTypeParameters(definition.ContractName);

				string genericAssemblyQualifiedName = definition.ContractName.Remove(definition.ContractName.IndexOf('['), definition.ContractName.LastIndexOf(']') - definition.ContractName.IndexOf('[') + 1);

				Expression<Func<ExportDefinition, bool>> expression =
					(e) => e.ContractName == genericAssemblyQualifiedName;
				ImportDefinition import = new ImportDefinition(expression, genericAssemblyQualifiedName, definition.Cardinality, definition.IsRecomposable, definition.IsPrerequisite);


				//Expression<Func<ExportDefinition, bool>> finderExpression =
				//    (finderE) => finderE.ContractName == genericDefinitionName;
				//ImportDefinition finderImport = new ImportDefinition(finderExpression, genericDefinitionName, definition.Cardinality, definition.IsRecomposable, definition.IsPrerequisite);
				var contractExport = this.SourceCatalog.GetExports(import);
				if (contractExport.Any() == false)
				{
					// 여기에서 새로운 import 로 하면 SourceProvider 에서 현재의 importDefinition 정보를 전달해서 객체를 제대로 찾지 못함.
					return this.SourceCatalog.GetExports(definition);
				}

				Type type = Type.GetType(contractExport.First().Item1.ToString()).MakeGenericType(
										GetGenericTypeParameters(definition.ContractName).ToArray());

				var exports = this.catalog.GetExports(import)
					.Where((item) => item.Item1.ToString() == type.AssemblyQualifiedName);

				if (exports.Any())
					return exports;

				//{exportDefinition => ((exportDefinition.ContractName = "ConsoleApplication1.IUMC`1[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]") && (exportDefinition.Metadata.ContainsKey("ExportTypeIdentity") && "ConsoleApplication1.IUMC`1[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]".Equals(exportDefinition.Metadata.get_Item("ExportTypeIdentity"))))}
				TypeCatalog typeCatalog = new TypeCatalog(type);
				this.catalog.Catalogs.Add(typeCatalog);

				return this.catalog.GetExports(import)
					.Where((item1) => item1.Item1.ToString() == type.AssemblyQualifiedName);
			}
			else if (definition.ContractName.Contains(BACK_QUOTE_GENERIC_ARITY))
			{
				// 제네릭 타입으로 질의를 할 경우
				// 1번째는 인터페이스 타입의 계약이므로 건너뛴다.
				return this.catalog.GetExports(definition).Skip(1);
			}

			return this.SourceCatalog.GetExports(definition);
		}

		public override IQueryable<ComposablePartDefinition> Parts
		{
			get { return this.SourceCatalog.Parts; }
		}
	}
}
