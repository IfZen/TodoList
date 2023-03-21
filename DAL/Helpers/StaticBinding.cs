using System;
using System.Linq.Expressions;

using TodoList.Business.Exceptions;

namespace TodoList.Persistence.Tools
{
    // From https://gitlab.com/Framework.Net/ApplicationBase project TechnicalTools

    /// <summary>
    /// Cette classe permet de transformer les propriétés d'un objet en chaine de caractères pour les composants qui utilisent la reflection.
    /// L'avantage est de disposer dans le code d'une liaison statique sur ses proprietés afin :
    /// - De ne pas avoir de chaine de caractère magique dans le code (notamment quand on s'abonne au INotifyPropertyChanged...)
    /// - De rendre la refactorisation du code plus sûr (et automatique).
    /// Voir Example dans My.Helpers.GuiTools.Test.GetMemberName_Tests
    /// </summary>
    public static class GetMemberName
    {
        #region Public

        /// Rappel : les expressions ne seront jamais évaluées !
        /// L'objet utilisé pour accéder à la propriété peut donc être null.
        public static string GetMemberNameFor<T>(this T instance, Expression<Func<T, object>> expression)
        {
            return GetAnyMemberName(expression.Body);
        }
        public static string For<T>(Expression<Func<T, object>> expression)
        {
            return GetAnyMemberName(expression.Body);
        }
        public static string For<T, T2>(Expression<Func<T, T2>> expression)
        {
            return GetAnyMemberName(expression.Body);
        }

        [Obsolete("Peut provoquer des erreurs... Voir CheckResult en bas", true)]
        public static string For(Expression<Func<object>> expression)
        {
            return GetAnyMemberName(expression.Body);
        }
        public static string For(Expression<Action> expression)
        {
            return GetAnyMemberName(expression.Body);
        }
        public static string For<T>(Expression<Func<T>> expression)
        {
            return GetAnyMemberName(expression.Body);
        }

        #endregion Public

        #region Private

        static string GetAnyMemberName(Expression expression)
        {
            if (expression == null)
                throw new ArgumentException("The expression cannot be null.");

            if (expression is MemberExpression)
            {
                string name = "";
                while (expression is MemberExpression)
                {
                    // Reference type property or field
                    var memberExpression = (MemberExpression)expression;
                    name = memberExpression.Member.Name + (name.Length == 0 ? "" : ".") + name;
                    expression = memberExpression.Expression;
                }
                CheckResult(name);
                return name;
            }

            if (expression is MethodCallExpression methodCallExpression) // Reference type method
            {
                CheckResult(methodCallExpression.Method.Name);
                return methodCallExpression.Method.Name;
            }


            if (expression is UnaryExpression unaryExpression) // Property, field of method returning value type
            {
                string result = GetMemberNameFromUnaryExpression(unaryExpression);
                CheckResult(result);
                return result;
            }

            throw new ArgumentException("Invalid expression");
        }

        private static string GetMemberNameFromUnaryExpression(UnaryExpression unaryExpression)
        {
            if (unaryExpression.Operand is MethodCallExpression methodExpression)
                return methodExpression.Method.Name;

            // Verifier si la ligne actuel ne serait pas necessaire quand la propriete n'a qu'un accesseur et pas de setteur
            return GetAnyMemberName(unaryExpression.Operand);
            // Ancienne ligne de code
            //return ((MemberExpression)unaryExpression.Operand).Member.Name;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void CheckResult(string returned_string)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
                // Si ce cas se produit, regardez quelle methode For a originellement été appelée,
                // et interdisez son utilisation si le type de retour de l'expression fonctionnelle est object.
                // "Pour interdire utiliser obsolete(\"...\", true), ne supprimez pas la methode afin d'eviter que quelqu'un la reajoute plus tard !
                // Por plus d'information sur ce type d'erreur, voir à http://stackoverflow.com/questions/3567857/why-are-some-object-properties-unaryexpression-and-others-memberexpression
            }
            else
                throw new TechnicalException("Unknown error!", null);
        }
        #endregion Private
    }

}
