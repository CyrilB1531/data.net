# statsmodels → .NET

**Verdict : à trancher.** Le socle (régression linéaire, distributions, tests de
base) existe ; l'**économétrie riche** (GLM détaillés, ARIMA/SARIMAX, modèles
mixtes, résumés type R avec p-values et intervalles) n'a **pas** de bon
équivalent .NET. C'est un candidat à du code natif *si votre usage le justifie*.

| Besoin statsmodels | .NET |
|---|---|
| Régression linéaire, moindres carrés | **Math.NET Numerics** (`Fit`, `MultipleRegression`) |
| Distributions, tests d'hypothèse de base | **Math.NET** (`Distributions`), Accord.NET |
| GLM avancés, séries temporelles, résumés économétriques | ⚠️ **manque** — à écrire ou à contourner |

```csharp
using MathNet.Numerics;

// OLS y = a + b·x
(double a, double b) = Fit.Line(xs, ys);
double r2 = GoodnessOfFit.RSquared(xs.Select(x => a + b * x), ys);
```

## Pièges

- **Pas de « summary() » riche.** Erreurs standard, IC, p-values des
  coefficients ne sont pas fournis clés en main : à calculer soi-même (matrice de
  covariance des estimateurs) ou à porter.
- **Séries temporelles.** Rien d'équivalent à `SARIMAX`/`statespace` : soit on
  restreint le périmètre, soit c'est un lot natif à part entière.

> Avant tout développement natif ici, confronter au **besoin réel** : souvent une
> régression + quelques tests suffisent, et Math.NET couvre déjà.

_Guide à étoffer au fil des besoins réels._
