# EasySave - Logiciel de sauvegarde ProSoft

## Vision globale

EasySave est un logiciel de sauvegarde distribue par ProSoft, avec une evolution progressive en 4 versions :

- **v1.0** : base console complete
- **v1.1** : extension de la v1.0 (format de log JSON/XML)
- **v2.0** : passage en interface graphique + chiffrement + contrainte logiciel metier
- **v3.0** : parallelisation, priorisation, pilotage temps reel et centralisation Docker

Ce README decrit **toutes les fonctionnalites attendues du projet**, puis distingue l'etat reel du depot.

## 1) Contexte et problematique

Les clients ProSoft ont besoin d'un outil de sauvegarde :

- fiable sur disque local, externe et lecteur reseau
- utilisable en francais et en anglais
- tracable en temps reel (etat + log)
- maintenable et evolutif version apres version

Le projet doit aussi repondre a des contraintes de qualite :

- code lisible, peu duplique, facilement testable
- architecture claire, orientee evolutions futures
- documentation utilisateur/support complete
- gestion Git rigoureuse sur toute la duree du fil rouge

## 2) Calendrier projet (rappel)

- **Livrable 1 (v1.0)** : cahier des charges, UML, code + docs
- **Livrable 2 (v1.1 + v2.0)** : UML, code + docs (non evalue mais structurant)
- **Livrable 3 (v3.0)** : UML, code final, soutenance

## 3) Fonctionnalites attendues par version

### v1.0 - Console

#### Fonctions metier

- application console .NET
- creation jusqu'a 5 travaux de sauvegarde
- un travail contient :
  - nom du travail
  - repertoire source
  - repertoire cible
  - type (`complete` ou `differentielle`)
- execution :
  - d'un seul travail
  - de plusieurs travaux en sequence
- execution via ligne de commande :
  - `EasySave.exe 1-3`
  - `EasySave.exe 1;3`

#### Compatibilite de stockage

- disques locaux
- disques externes
- lecteurs reseau

#### Log journalier (obligatoire)

- ecriture en temps reel des actions
- informations minimales :
  - horodatage
  - nom du travail
  - chemin complet source (UNC)
  - chemin complet destination (UNC)
  - taille du fichier
  - temps de transfert en ms (negatif si erreur)
- format JSON lisible (retours ligne/indentation)
- cette brique doit etre dans une DLL dediee `EasyLog.dll`

#### Fichier d'etat temps reel (obligatoire)

- fichier unique de suivi de l'avancement
- informations minimales par travail :
  - nom du travail
  - horodatage de la derniere action
  - etat (`Active`, `Inactive`, etc.)
  - nombre total de fichiers eligibles
  - taille totale a transferer
  - progression
  - nombre de fichiers restants
  - taille restante
  - fichier source en cours
  - fichier destination en cours
- format JSON lisible (retours ligne/indentation)

### v1.1 - Console (retro-compatible v1.0)

- toutes les fonctions v1.0 conservees
- nouveaute :
  - choix du format du log journalier : **JSON ou XML**
- objectif : satisfaire un client qui ne migre pas vers v2.0

### v2.0 - Graphique

#### Evolution d'interface

- abandon du mode console principal
- application graphique (WPF/Avalonia ou equivalent)
- commandes de sauvegarde equivalentes a v1.0

#### Evolution metier

- nombre de travaux de sauvegarde **illimite**
- integration du logiciel externe **CryptoSoft**
  - chiffrement uniquement pour les extensions configurees
- log journalier enrichi :
  - ajout du **temps de chiffrement** en ms
    - `0` : pas de chiffrement
    - `>0` : chiffrement effectue
    - `<0` : erreur chiffrement
- detection d'un logiciel metier :
  - si detecte, lancement d'une sauvegarde interdit
  - si sequence deja lancee, terminer le fichier en cours puis arreter
  - l'arret doit etre journalise

#### Point important

- des commandes Play/Pause/Stop par travail sont demandees par les clients
- non exigees en v2.0, mais doivent etre preparees pour v3.0

### v3.0 - Graphique avancee

#### Orchestration des sauvegardes

- abandon du mode purement sequentiel
- execution des travaux **en parallele**

#### Regles de priorite

- extensions prioritaires configurees dans les parametres
- aucun fichier non prioritaire ne doit partir tant qu'il reste du prioritaire sur au moins un travail

#### Controle des gros transferts

- interdiction de transferer en parallele 2 fichiers > `n` Ko
- `n` configurable
- pendant un gros transfert, les petits fichiers peuvent continuer (si regles priorite respectees)

#### Pilotage temps reel utilisateur

- commandes par travail ou globales :
  - `Play` (demarrage/reprise)
  - `Pause` (effective apres fichier en cours)
  - `Stop` (arret immediat)
- suivi de progression temps reel (au minimum pourcentage)

#### Logiciel metier en cours d'execution

- si detecte : mise en pause automatique de tous les travaux
- reprise automatique quand le logiciel metier se ferme

#### CryptoSoft mono-instance

- CryptoSoft ne peut tourner qu'en un seul exemplaire
- EasySave doit gerer cette contrainte sans corruption de flux

#### Centralisation des logs (Docker)

- service Docker de centralisation temps reel
- modes possibles :
  - local uniquement
  - centralise uniquement
  - local + centralise
- en mode centralise : un unique fichier journalier mutualise, avec identification utilisateur/machine

## 4) Contraintes transverses du projet

- langage : C#
- framework cible pedagogique : .NET 8.0 (selon cahier des charges)
- outils : Visual Studio, GitHub, UML (ArgoUML recommande)
- tout document/code/commentaire exploitable par des equipes anglophones
- fonctions courtes, faible duplication, conventions de nommage strictes
- release notes obligatoires
- manuel utilisateur : 1 page
- documentation support : prerequis, emplacement installation, config, logs, etat

## 5) Architecture cible recommandee

- couche **Core/Domain** : modeles et regles metier pures
- couche **Application** : orchestration des jobs, priorites, pause/play/stop
- couche **Infrastructure** :
  - acces systeme fichiers
  - logs JSON/XML + centralisation
  - integration CryptoSoft
  - detection logiciel metier
- couche **Presentation** :
  - Console (v1.x)
  - GUI MVVM (v2/v3)

Cette separation reduit les couts des futures versions et limite les regressions.

## 6) Etat actuel du depot (important)

Le depot present contient une base fonctionnelle surtout orientee console :

- sauvegardes complete/differentielle
- gestion FR/EN
- log JSON
- etat temps reel JSON
- tests unitaires en place

Fonctionnalites encore a implementer selon roadmap globale :

- format XML (v1.1)
- interface graphique (v2+)
- CryptoSoft (v2+)
- detection/pause logiciel metier (v2/v3)
- parallelisme, priorites, controle gros fichiers (v3)
- centralisation Docker des logs (v3)

## 7) Prise en main (depot actuel)

```bash
dotnet restore
dotnet build
dotnet run --project Console
dotnet test
```

## 8) Livrables documentaires a maintenir

- UML a rendre avant chaque livrable
- manuel utilisateur (1 page)
- guide support technique
- release note par version
- compte-rendu d'evolutions envisagees v4.0 (benefice client vs cout dev)
