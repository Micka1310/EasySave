# EasySave - Logiciel de sauvegarde ProSoft

# Sommaire
- [Vision globale](#vision-globale)
- [Contexte et problématique](#contexte-et-problématique)
- [Fonctionnalités attendues par version](#fonctionnalités-attendues-par-version)
- [Bonne pratique de développement appliquer](#Bonne-pratique-de-développement-appliquer)
- [Pipeline de développement du dépôt](#pipeline-de-développement-du-dépôt)
- [Quelque commandes utiles durant le développement](#quelque-commandes-utiles-durant-le-développement)
- [Livrables à rendre](#livrables-à-rendre)

# Vision globale

EasySave est un logiciel de sauvegarde distribué par ProSoft, avec une evolution progressive en 4 versions :

- **v1.0** : base console complete
- **v1.1** : extension de la v1.0 (format de log JSON/XML)
- **v2.0** : passage en interface graphique + chiffrement + contrainte logiciel metier
- **v3.0** : parallelisation, priorisation, pilotage temps reel et centralisation Docker

Ce README decrit **toutes les fonctionnalités attendues du projet**, puis distingue l'état réel du dépôt.

Outils utilisé durant ce projet : Visual Studio, GitHub, UML

# Contexte et problématique

Les clients ProSoft ont besoin d'un outil de sauvegarde :
- fiable sur disque local, externe et lecteur réseau
- utilisable en francais et en anglais
- tracable en temps réel (état + log)
- maintenable et évolutif version apres version

Le projet doit aussi répondre à ces contraintes de qualité :
- code lisible, peu dupliqué, facilement testable
- architecture claire, orientée évolutions futures
- documentation utilisateur/support complete
- gestion Git rigoureuse sur toute la durée du fil rouge

**L'objectif principal** de ce projet est donc d'utiliser de bonne pratiques de développement afin de réduire les coûts de développement des futures versions.

Cela nous permettra aussi de réagir rapidement à la remontée éventuelle d'un dysfonctionnement.
Plusieurs versions sont donc développer pour ce projet.

# Fonctionnalités attendues par version

## v1.0 - Application console

### Technologies utilisées

Language utilisé : C#

Framework de programmation logiciel utilisé : .NET 8 ou plus

Moteur de test : MSTest

### Fonctions metier

La **v1** est une application console.

La **v1** de l'application doit comprendre :

- un affichage console
- une limite de création jusqu'a **5 travaux** de sauvegarde
- un travail qui contient :
  - un nom de travail
  - le chemin du répertoire source
  - le chemin du répertoire cible
  - un type (`complete` ou `differentielle`)
- une option pour éxecuter :
  - un seul travail
  - plusieurs travaux en séquence
- l'éxecution via ligne de commande :
  - Exemple 1 pour exécuter le travail 1 et 3 : `EasySave.exe 1-3`
  - Exemple 2 pour exécuter les travaux de 1 à 3 : `EasySave.exe 1;3`
- la possibilité de sauvegarder dans plusieurs type de stockage :
  - disques locaux
  - disques externes
  - lecteurs reseau
- l'écriture un **fichier log** unique avec :
  - une écriture en temps réel des actions
  - des informations minimales :
    - horodatage
    - nom du travail
    - chemin complet source (UNC)
    - chemin complet destination (UNC)
    - taille du fichier
    - temps de transfert en ms (negatif si erreur)
  - un format JSON lisible (retours ligne/indentation)
  - l'obligation dêtre implémenter dans une DLL dediée (Exemple : `EasyLog.dll`)
- l'écriture dun **fichier détat** unique avec :
  - les informations minimales par travail :
  - le nom du travail
  - l'horodatage de la dernière action
  - l'état (`Active`, `Inactive`, etc.)
  - le nombre total de fichiers eligibles
  - la taille totale a transferer
  - la progression
  - le nombre de fichiers restants
  - la taille restante
  - le fichier source en cours
  - le fichier destination en cours
  - en format JSON lisible (retours ligne/indentation)
 
### Architecture logique de lapplication

À partir de la racine du projet, nous avons :
\\\\\\ À Compléter \\\\\

## v1.1 - Application Console (retro-compatible avec la v1.0)

La **v1.1** est une application console similaire à la v1 avec en plus la possibilité de choisir le format du fichier log journalier en JSON ou XML.

La **v1.1** à pour but de satisfaire un client qui ne migre pas vers v2.0.

### Changement apporté

Tous les préréquis de la **v1.0** conservés.

La **v1.1** de lapplication a comme nouveauté :
- la possibilité de choisir le format du log journalier : **JSON ou XML**

### Architecture logique de lapplication

À partir de la racine du projet, nous avons :
\\\\\\ À Compléter \\\\\

## v2.0 - Application graphique

À partir de la v2, l'application passe dun **affichage console** à un **affichage graphique**.

### Technologies utilisées

Language utilisé : C#

Framework de programmation logiciel utilisé : .NET 8 ou plus

Moteur de test pour les tests unitaires : MSTest

Framework utilisé pour l'affichage graphique : WPF

### Changement apporté

Le passage de la **v1.1** à la **v2.0** implique :
- l'abandon de affichage console pour passer à laffichage graphique
- un nombre de travaux de sauvegarde **illimité** au lieu de rester à 5 travaux maximum
- une option de chiffrement et une autre pour le déchiffrement. Ces fonctinnalités doivent sappliquer uniquement pour les sauvegardes indiqué par lutilisateur.
- le même log journalier enrichi qui comprend comme nouveauté :
  - un ajout du **temps de chiffrement** en ms avec ces affichages :
    - `0` : pas de chiffrement
    - `>0` : chiffrement effectue
    - `<0` : erreur chiffrement
- une intégration dun logiciel métier au choix de lutilisateur
- la détection du logiciel metier avec ces comportements :
  - si detecté, on interdit le lancement de quelquonque sauvegarde.
  - si une sauvegarde de séquence est déjà lancée, on termine le fichier en cours puis on arrête les prochaines sauvegarde
- des commandes Play/Pause/Stop par travail sont demandees par les clients (non fonctionelle pour l'instant) pour la préparation à la **v3**

### Architecture logique de l'application

À partir de la racine du projet, nous avons :
\\\\\\ À Compléter \\\\\

## v3.0 - Application graphique avancée

Amélioration de l'application graphique avec gestion de processus intégré et isolation avec Docker.

### Technologies utilisées

Similaire à la **v2**

### Changement apporté

Le passage de la **v2.0** à la **v3.0** implique :
- l'abandon du mode purement sequentiel
- l'exécution des travaux **en parallèle**
- l'extensions prioritaires configurées dans les parametres
- qu'aucun fichier non prioritaire ne doit être sauvegardé tant qu'il reste au moins un travail prioritaire non-sauvegardé
- l'interdiction de transferer en parallèle 2 fichiers > `n` Ko
- que la taille `n` Ko soit configurable
- que lors de la sauvegarde d'un gros fichier, des petits fichiers peuvent être sauvegarder en parralèle si les règles de priorité sont respectées
- de possible interaction durant la sauvegarde d'un travail :
  - `Play` (démarrage/reprise)
  - `Pause` (pause effective après fichier en cours)
  - `Stop` (arrêt immediat)
- la suivi de progression temps reel (au minimum pourcentage)
- que si le logiciel métier est détecter, mise en pause automatique de tous les travaux
- la reprise automatique quand le logiciel metier se ferme
- que l'application **Cryptosoft** ne peut tourner qu'en un seul exemplaire
- un service Docker de centralisation en temps réel avec plusieurs modes possibles :
  - local uniquement
  - centralise uniquement
  - local + centralise
- qu'un seul et unique fichier journalier doit existé

### Architecture logique de l'application

À partir de la racine du projet, nous avons :
\\\\\\ À Compléter \\\\\

# Bonne pratique de développement appliquer

Voici les **bonnes pratiques** que nous avons utilisé pour rendre le code de l'application évolutive et maintenable :
- Architecture MVVM pour toutes les versions
- Design pattern de stratégie appliquer pour la logique de choix d'option (partie ViewModel)
- Tests unitaires implémenter depuis la racine dans le dossier `UnitTest` pour tester les fonctionnalités de l'application et pour l'aide au débogage
- Modularité des codes pousser au maximum pour une meilleure maintenance du code

# Pipeline de développement du dépôt

Le dépôt présent contient comprend plusieurs branches particulières.

Les **branches principaux** qui sont utilisé durant le développement sont :
- `main` : Contient la version actuelle de l'application
- `production` : Contient le prototype de l'application en cours de développement

Les autres branches posté dans le dépôt par les membres de l'équipe sont utiliser pour suivre l'avancement de leurs travaux.

Nous utilisons des **pull request** pour valider et intégrer toute modification à l'application.

La pipeline de développement durant le projet se déroule de cette façon :
- Un développeur finit de développer une fonctionnalité et push son code dans ce dépôt en ligne
- Ce même développeur poste un pull request
- Un autre développeur de l'équipe vérifie les changements effectuer par le développeur qui est l'auteur depuis la pull request posté
- Si les modifications sont correcte, le développeur qui vérifie la pull request la valide et les modifications s'applique vers la branche `production`
- Les autres développeurs peuvent se mettre à jour localement avec les modifications apporté dans la branche `production` en faisant un pull.
- Si tout fonctionne correctement, un pull request se fait pour appliqué les modifications de la branche `production` vers la branche `main`

En résumé, lorsque tout se passe bien, la **pipeline** permet à chaque modification durant le développement de l'application de suivre ce chemin de versionning :
`branche personnelle` -> `production` -> `main`


# Quelque commandes utiles durant le développement

**Lignes de commandes** à utiliser depuis un **terminal** lors du développement de l'application :
```bash
dotnet build     # pour compiler le code
dotnet run     # pour démarrer l'application
dotnet test    # pour démarrer les test unitaires
```

# Livrables à rendre

D'autres livrables seront aussi rendu séparément à savoir :
- un **UML** pour chaque version de l'application
- un **manuel utilisateur (1 page)** à la fin du développement de l'application
