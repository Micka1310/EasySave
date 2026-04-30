# EasySave - Fichier de suivi de projet

> ⚠️ Ce fichier est à mettre à jour après chaque session de travail pour assurer la continuité entre les membres de l'équipe.

---

## 1. Contexte du projet

**Éditeur :** ProSoft  
**Projet :** EasySave — Logiciel de sauvegarde  
**Équipe :** 4 développeurs  
**Prix unitaire :** 200 € HT  
**Maintenance annuelle :** 12% du prix d'achat (contrat tacite reconduction, indice SYNTEC)

### Outils imposés
- Visual Studio 2022 ou supérieur (actuellement VS 2026 18.6.0-insiders)
- GitHub : https://github.com/Micka1310/EasySave (branche `main`)
- UML : ArgoUML
- Langage : **C#** / **.NET 10**

### Contraintes de code
- Code, commentaires et documents **en anglais** (filiales anglophones)
- Fonctions courtes, pas de duplication de code
- Respect des conventions de nommage
- IHM soignée

---

## 2. Livrables

### Version 1.0 (en cours)
Application **console** .NET avec :

#### Travaux de sauvegarde
- Maximum **5 travaux**
- Chaque travail : `nom`, `répertoire source`, `répertoire cible`, `type` (complète ou différentielle)
- Supports : disques locaux, externes, lecteurs réseau
- Sauvegarde récursive (fichiers + sous-répertoires)
- Multilingue : **FR** et **EN**

#### Ligne de commande
- `EasySave.exe 1-3` → exécute les sauvegardes 1 à 3
- `EasySave.exe 1;3` → exécute les sauvegardes 1 et 3

#### Fichier Log journalier (`YYYY-MM-DD.json`)
Écrit en temps réel via la DLL **EasyLog.dll** (projet `Lib`) :
- Horodatage
- Nom de sauvegarde
- Adresse source (format UNC)
- Adresse destination (format UNC)
- Taille du fichier
- Temps de transfert en ms (négatif si erreur)

#### Fichier État temps réel (`state.json`)
Mis à jour en temps réel :
- Nom du travail
- Horodatage dernière action
- État (Actif / Non Actif)
- Si actif : total fichiers, taille totale, progression, fichiers restants, taille restante, source/destination en cours

#### Règles fichiers
- Format **JSON** avec retours à la ligne (lisible Notepad)
- Emplacement **non codé en dur** (pas de `C:\temp\`)
- Fichiers de config également en JSON

#### DLL EasyLog
- Projet séparé dans Git
- Toutes les évolutions doivent rester **compatibles v1.0**

### Version 2.0 (future)
- Interface graphique
- Architecture **MVVM** (à anticiper dès la v1.0)

---

## 3. Architecture — Diagramme de classes

Architecture **MVVM + Pattern Stratégie** :

### 🔵 Model
| Classe | Rôle | Fichier | Statut |
|--------|------|---------|--------|
| `Work` | Entité travail (nom, source, destination, type) + getters | `Lib/Model/Work.cs` | ✅ Fait |
| `WorkList` | Liste de max 5 `Work` (AddWork, GetWork) | `Lib/Model/WorkList.cs` | ✅ Fait |
| `Language` | Langue courante (EN par défaut), ModifyLanguage() | — | ❌ À faire |
| `LogFile` | Log journalier JSON (`YYYY-MM-DD.json`) | `Lib/Model/LogFile.cs` | ✅ Fait + testé |
| `StateFile` | État temps réel JSON (`state.json`) | `Lib/Model/StateFile.cs` | ✅ Fait + testé |

### 🟢 ViewModel
| Classe | Rôle | Fichier | Statut |
|--------|------|---------|--------|
| `Controller` | Reçoit les inputs, sélectionne la stratégie, contient `WorkList` + `Language` | `Console/ViewModel/Controller.cs` | ✅ Fait |

### 🟣 Pattern Stratégie
| Classe | Rôle | Dépendance | Fichier | Statut |
|--------|------|------------|---------|--------|
| `IStrategy` (interface) | Contrat : `option`, `parameterMessage`, `Execution()` | — | `Console/ViewModel/Strategy.cs` | ✅ Fait |
| `DisplayWork1` | Afficher les travaux | — | `Console/ViewModel/Strategy.cs` | ✅ Fait |
| `CreateWork2` | Créer un travail de sauvegarde | `LogFile` + `StateFile` | `Console/ViewModel/Strategy.cs` | ✅ Fait + intégré |
| `ExecuteWork3` | Exécuter un ou plusieurs travaux | `LogFile` + `StateFile` | `Console/ViewModel/Strategy.cs` | ✅ Fait + testé |
| `ChangeLanguage4` | Changer la langue | — | `Console/ViewModel/Strategy.cs` | ❌ Vide |

### 🟠 View
| Classe | Rôle | Fichier | Statut |
|--------|------|---------|--------|
| `ConsoleView` | Affiche le menu, récupère l'input, envoie au `Controller` | — | ❌ À faire |

---

## 4. Structure actuelle du projet

```
EasySave/
├── Console/
│   ├── Console.csproj
│   ├── Program.cs                  ← Point d'entrée (code de test provisoire)
│   └── ViewModel/
│       ├── Controller.cs           ← ✅ Implémenté
│       └── Strategy.cs             ← ✅ IStrategy, DisplayWork1, CreateWork2, ExecuteWork3, ChangeLanguage4
├── Lib/
│   ├── Lib.csproj
│   ├── Library.cs                  ← Code de test provisoire (à supprimer)
│   └── Model/
│       ├── Work.cs                 ← ✅ Implémenté
│       ├── WorkList.cs             ← ✅ Implémenté
│       ├── LogFile.cs              ← ✅ Implémenté + testé
│       └── StateFile.cs            ← ✅ Implémenté + testé
├── UnitTest/
│   ├── UnitTest.csproj
│   ├── MSTestSettings.cs
│   ├── Test1.cs                    ← Tests provisoires (à supprimer)
│   ├── ConsoleController.cs        ← ✅ Tests Controller (GetOption, GetParameter, ExecuteOption)
│   ├── Strategies/
│   │   └── ExecuteWork3Tests.cs    ← ✅ 6 tests (parsing, sauvegarde complète, différentielle, erreur)
│   └── Lib/
│       ├── LogFileTests.cs         ← ✅ 3 tests (tous réussis)
│       └── StateFileTests.cs       ← ✅ 3 tests (tous réussis)
└── CONTEXT.md                      ← Ce fichier
```

---

## 5. État d'avancement

### ✅ Fait
- Création du dépôt GitHub
- Structure de la solution avec 3 projets (Console, Lib, UnitTest)
- Diagramme de classes Mermaid validé par l'équipe (v2 avec suppresssion de `ModelList`)
- Cadre de tests unitaires en place (MSTest)
- `Work.cs` — entité travail avec getters
- `WorkList.cs` — liste de travaux (max 5 non encore contrôlé)
- `Controller.cs` — sélection et exécution des stratégies
- `Strategy.cs` — `IStrategy`, `DisplayWork1`, `CreateWork2` ✅ (LogFile+StateFile intégrés), `ExecuteWork3` ✅ (sauvegarde complète + différentielle), `ChangeLanguage4` (vide)
- `LogFile.cs` — log journalier JSON ✅ **15/15 tests réussis**
- `StateFile.cs` — état temps réel JSON ✅ **15/15 tests réussis**
- Tests `ConsoleController.cs` (GetOption, GetParameter, ExecuteOption)
- Tests `ExecuteWork3Tests.cs` — 6 tests (parsing, sauvegarde complète, différentielle, erreur)
- Tests `LogFileTests.cs` + `StateFileTests.cs`

### 🔄 En cours
- Push sur la branche `production`

### ❌ À faire
- [ ] Implémenter `Language` (gestion FR/EN)
- [ ] Implémenter `ChangeLanguage4.Execution()`
- [ ] Implémenter `ConsoleView` avec menu interactif
- [ ] Gestion des arguments en ligne de commande (`1-3`, `1;3`) dans `Program.cs`
- [ ] Contrôle max 5 travaux dans `WorkList`
- [ ] Supprimer `Library.cs` et `Test1.cs` (code provisoire)
- [ ] Documentation utilisateur (1 page)
- [ ] Release note v1.0
- [ ] Documentation support technique

---

## 6. Décisions techniques

| Décision | Détail |
|----------|--------|
| Framework | .NET 10 (au lieu de .NET 8 imposé, validé avec le responsable) |
| DLL log | Projet `Lib` → sera renommé/configuré en `EasyLog.dll` |
| Format config | JSON avec retours à la ligne |
| Emplacement fichiers | `AppContext.BaseDirectory` (à côté de l'exécutable), jamais `C:\temp\` |
| Pattern | MVVM + Stratégie pour anticiper la v2.0 GUI |
| `ModelList` supprimé | `WorkList` et `Language` sont directement dans `Controller` |
| `IStrategy.Execution()` | Reçoit `WorkList` directement (plus `ModelList`) |
| Accès fichiers concurrent | `lock (fileLock)` dans `LogFile` et `StateFile` pour éviter les conflits multi-thread |
| Commentaires | En français (décision équipe)

---

## 7. Procédures utiles

### Lancer les tests unitaires

```powershell
# Tous les tests de la solution avec détails complets
dotnet test --logger "console;verbosity=detailed"

# Un projet spécifique
dotnet test UnitTest/UnitTest.csproj --logger "console;verbosity=detailed"

# Un test spécifique par nom de méthode
dotnet test --filter "FullyQualifiedName~TestMethod1" --logger "console;verbosity=detailed"

# Une classe de test spécifique
dotnet test --filter "ClassName~Test1" --logger "console;verbosity=detailed"

# Uniquement les tests qui échouent
dotnet test --filter "Outcome=Failed" --logger "console;verbosity=detailed"
```

### Niveaux de verbosité

| Niveau | Affichage |
|--------|-----------|
| `quiet` | Résultat global uniquement |
| `minimal` | Résumé + erreurs |
| `normal` | Résumé + tests échoués détaillés |
| `detailed` | Tout : chaque test, durée, output Console/Trace |
| `diagnostic` | Tout + infos système et configuration |

> ⚠️ Les `Console.WriteLine()` et `Trace.WriteLine()` dans les tests s'affichent **uniquement** avec `detailed` ou `diagnostic`.

### Convention de tests à adopter (AAA)
```csharp
// Arrange — préparer les données
// Act     — exécuter l'action
// Assert  — vérifier le résultat
```

### Organisation des tests unitaires

Tous les tests sont dans le projet `UnitTest/`, organisés par couche :

```
UnitTest/
├── MSTestSettings.cs
├── Test1.cs                    ← Tests provisoires (à supprimer après implémentation)
│
├── Model/
│   ├── WorkTests.cs
│   ├── WorkListTests.cs
│   ├── LanguageTests.cs
│   └── ModelListTests.cs
│
├── ViewModel/
│   └── ControllerTests.cs
│
├── Strategies/
│   ├── DisplayWorks1Tests.cs
│   ├── CreateWorks2Tests.cs
│   ├── ExecWork3Tests.cs
│   └── ChangeLanguage4Tests.cs
│
└── Lib/
    ├── LogFileTests.cs
    └── StateFileTests.cs
```

### Règles de nommage des tests
- **1 fichier de test** par classe métier
- **Nommage fichier** : `NomDeLaClasse`**Tests**.cs
- **Nommage méthode** : `NomMéthode_Scénario_RésultatAttendu`

```csharp
// Exemples
public void AddWork_WhenListFull_ShouldThrowException()
public void AddWork_ValidInput_ShouldAddToList()
```

---

## 9. Stratégie des branches Git

| Branche | Rôle | Règle |
|---------|------|-------|
| `main` | Code stable et validé | ❌ Push direct interdit — PR obligatoire |
| `production` | Intégration du travail de l'équipe | ❌ Push direct interdit — PR obligatoire |
| `ConsoleStrategy` | Travail du collègue (Controller, Strategy) | Base pour `EasyLogWorkJob` |
| `EasyLogWorkJob` | **Votre branche** (LogFile, StateFile, ExecuteWork3) | PR vers `production` |
| `initial` | Branche initiale | — |

### Procédure pour créer et pusher `EasyLogWorkJob`
```sh
# 1. Récupérer et basculer sur ConsoleStrategy
git fetch origin
git checkout ConsoleStrategy
git pull origin ConsoleStrategy

# 2. Créer EasyLogWorkJob depuis ConsoleStrategy
git checkout -b EasyLogWorkJob

# 3. Ajouter les fichiers
git add Lib/Model/LogFile.cs
git add Lib/Model/StateFile.cs
git add Console/ViewModel/Strategy.cs
git add UnitTest/Lib/LogFileTests.cs
git add UnitTest/Lib/StateFileTests.cs
git add UnitTest/Strategies/ExecuteWork3Tests.cs
git add .gitignore

# 4. Committer et pusher
git commit -m "Add LogFile, StateFile, ExecuteWork3 with unit tests"
git push origin EasyLogWorkJob

# 5. Créer une Pull Request sur GitHub
# Base : production  ←  Compare : EasyLogWorkJob
```

### ⚠️ Règle importante
- `CONTEXT.md` est dans `.gitignore` → reste **uniquement en local**, jamais pushé

---

## 10. Notes de session

| Date | Membre | Action |
|------|--------|--------|
| — | — | Création du dépôt GitHub par un membre de l'équipe |
| — | — | Diagramme de classes Mermaid v1 validé |
| — | — | Création de ce fichier CONTEXT.md |
| — | — | Analyse de la structure du projet (squelette vide, code de test provisoire) |
| — | — | Documentation des commandes de test `dotnet test` |
| — | — | Décision : 1 fichier de test par classe, organisés par couche dans `UnitTest/` |
| — | — | Décision : tests créés en parallèle de l'implémentation de chaque classe |
| — | — | Git pull branche `production` — récupération du code du collègue (`Work`, `WorkList`, `Controller`, `Strategy`) |
| — | — | Diagramme de classes v2 : suppression de `ModelList`, `LogFile`+`StateFile` → `CreateWork2` |
| — | — | Implémentation de `LogFile.cs` + `StateFile.cs` avec `lock` anti-concurrent |
| — | — | 9/9 tests réussis (`LogFileTests` + `StateFileTests`) |
| — | — | Intégration de `LogFile` + `StateFile` dans `CreateWork2.Execution()` ✅ |
| — | — | Implémentation de `ExecuteWork3` (sauvegarde complète + différentielle + gestion erreurs) |
| — | — | 15/15 tests réussis après implémentation de `ExecuteWork3` |
| — | — | `production` protégée → push direct interdit, PR obligatoire |
| — | — | `CONTEXT.md` ajouté au `.gitignore` (reste en local uniquement) |
| — | — | Décision : branche `EasyLogWorkJob` créée depuis `ConsoleStrategy` |
| — | — | Prochaine étape : créer et pusher la branche `EasyLogWorkJob` |
