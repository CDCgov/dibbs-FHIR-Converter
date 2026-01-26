using System.Collections.Generic;
using System.Globalization;
using Fluid.Ast;
using Fluid.Parser;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace Dibbs.Fhir.Liquid.Converter
{
    public static class EvaluateParser
    {
        // Please note: the evaluate tag currently only supports passing one argument to the template you're evaluating
        public static readonly Parser<EvaluateStatement> Parser =
            Terms.Identifier() // The name of the variable that we're assigning to
                .AndSkip(Terms.Text("using")) // ignore the "using" keyword
                .And(Terms.String()) // The name of the template we want to render
                .And(Argument()) // The parameter key/value pair
                .Then(result =>
                {
                    var target = result.Item1.ToString();
                    var template = result.Item2.ToString();
                    var attributes = new Dictionary<string, Expression>()
                    {
                    { result.Item3.Key, result.Item3.Value },
                    };

                    return new EvaluateStatement(target, template, attributes);
                });

        private static Parser<string> FluidIdentifier()
        {
            return SkipWhiteSpace(new IdentifierParser()).Then(x => x.ToString());
        }

        private static Parser<KeyValuePair<string, Expression>> Argument()
        {
            var parameter = Terms.Identifier() // Parameter name
                .AndSkip(Terms.Text(":")) // Ignore ":"
                .And(Member()) // Parameter value, we use a MemberExpression so Fluid can resolve the value
                .Then(r => new KeyValuePair<string, Expression>(r.Item1.ToString(), r.Item2));

            parameter.Name = "Parameter";
            return parameter;
        }

        private static Parser<MemberSegment> Indexer()
        {
            var indexer = Between(Literals.Char('['), Deferred<Expression>(), Terms.Char(']')).Then<MemberSegment>(x => new IndexerSegment(x));
            indexer.Name = "Indexer";

            return indexer;
        }

        private static Parser<MemberExpression> Member()
        {
            var member = FluidIdentifier().Then<MemberSegment>(x => new IdentifierSegment(x)).And(
                    ZeroOrMany(
                        Literals.Char('.').SkipAnd(
                            FluidIdentifier().Or(Terms.Integer(NumberOptions.None).Then(x => x.ToString(CultureInfo.InvariantCulture)))
                                .Then<MemberSegment>(x => new IdentifierSegment(x)))
                        .Or(Indexer())))
                    .Then(x => new MemberExpression([x.Item1, .. x.Item2]));

            member.Name = "Member";
            return member;
        }
    }
}