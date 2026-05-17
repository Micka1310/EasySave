# EasySave - Logiciel de sauvegarde ProSoft

# Sommaire
- [Vision globale](#vision-globale)
- [Contexte et problématique](#contexte-et-problématique)
- [Fonctionnalités attendues par version](#fonctionnalités-attendues-par-version)
- [Bonne pratique de développement appliquer](#bonne-pratique-de-développement-appliquer)
- [Pipeline de développement du dépôt en ligne](#pipeline-de-développement-du-dépôt-en-ligne)
- [Quelque commandes utiles durant le développement](#quelque-commandes-utiles-durant-le-développement)
- [Livrables divers à rendre](#livrables-divers-à-rendre)

# Vision globale

EasySave est un logiciel de sauvegarde distribué par ProSoft, avec une evolution progressive en 4 versions :

- **v1.0** : base console complète
- **v1.1** : extension de la v1.0 (format de log JSON/XML)
- **v2.0** : passage en interface graphique + chiffrement + contrainte logiciel metier
- **v3.0** : parallélisation, priorisation, pilotage temps réel et centralisation Docker

Ce README decrit **toutes les fonctionnalités attendues du projet**, puis distingue l'état réel du dépôt.

Outils utilisé durant ce projet : Visual Studio, GitHub, UML

# Contexte et problématique

Notre équipe vient d'intégrer l'éditeur de logiciels ProSoft.

Nous avons la responsabilité de gérer le projet “EasySave” qui consiste à développer un logiciel de sauvegarde.

En effet, les clients ProSoft ont besoin d'un outil de sauvegarde qui est :
- fiable sur disque local, externe et lecteur réseau
- utilisable en francais et en anglais
- tracable en temps réel (état + log)
- maintenable et évolutif version apres version

Le projet doit aussi répondre à ces contraintes de qualité :
- code lisible, peu dupliqué, facilement testable
- architecture claire, orientée évolutions futures
- documentation utilisateur/support complete
- gestion Git rigoureuse durant tout le développement du projet

**L'objectif principal** de ce projet est donc d'utiliser les bonnes pratiques de développement afin de réduire les coûts de développement des futures versions.

Cela nous permettra aussi de réagir rapidement à la remontée éventuelle d'un dysfonctionnement.
Plusieurs versions sont donc développer pour ce projet.

# Technologies utilisées

- **Visual Studio** : Éditeur de code qui permet facilement de créer des applications bureautique, mobile ainsi que des services web. Natif avec le framewok .NET 10
- **Language utilisé** : C#
- **Framework de programmation logiciel utilisé** : .NET 10 ou plus
- **Moteur de test** : MSTest
- **Framework utilisé pour l'affichage graphique** : WPF
- **Pour la simulation de serveurs** : Docker

# Fonctionnalités attendues par version

## v1.0 - Application console

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

## v1.1 - Application Console (retro-compatible avec la v1.0)

La **v1.1** est une application console similaire à la v1 avec en plus la possibilité de choisir le format du fichier log journalier en JSON ou XML.

La **v1.1** à pour but de satisfaire un client qui ne migre pas vers v2.0.

### Changement apporté

Tous les préréquis de la **v1.0** conservés.

La **v1.1** de lapplication a comme nouveauté :
- la possibilité de choisir le format du log journalier : **JSON ou XML**

## v2.0 - Application graphique

À partir de la v2, l'application passe dun **affichage console** à un **affichage graphique**.

### Changement apporté

Le passage de la **v1.1** à la **v2.0** implique :
- l'abandon de affichage console pour passer à laffichage graphique
- un nombre de travaux de sauvegarde **illimité** au lieu de rester à 5 travaux maximum
- une option de chiffrement et une autre pour le déchiffrement. Ces fonctinnalités doivent sappliquer uniquement pour les sauvegardes indiqué par l'utilisateur.
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

## v3.0 - Application graphique avancée

Amélioration de l'application graphique avec gestion de processus intégré et isolation avec Docker.

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

# Quelques modifications supplémentaire ajouté pour l'application

- Les travaux sont conservés même lorsque l'application s'éteint grâce à un fichier JSON nommé « work.json » depuis le chemin suivant : *"C:\Users\Nom_Utilisateur\AppData\Roaming\EasySave\"*
- Nous avons ajouté une option **"Supprimer un travail"** pour permettre à l’utilisateur de supprimer un travail qu’il n’a plus besoins. En particulier très utile pour la **version 1.0** de l’application du fait que nous n’avons le droit de conservé que **5 travaux au maximum**.
- La navigation du menu de l’application se fait avec des **flèches directionnelles**. Pour choisir une option ou valider une saisie, il faut appuyer sur la touche « `enter` ». Il est aussi possible de revenir en arrière en appuyant sur le bouton « `echap` ».
- Nous avons ajouté une option "*Exit / Quitter*" pour une meilleur expérience utilisateur.
- Nous avons ajouté une variable "*Success*" vérifiant pour chaque action si le transfert d'un fichier spécifique a été réussis depuis le **fichier log**.
- Nous avons ajouté un moyen de consulter plus facilement le **fichier log** pour que l’utilisateur n’ai pas à consulter manuellement le fichier journalier (c’est-à-dire, se diriger vers le chemin où se trouve le fichier journalier) en l’ouvrant par lui-même. Cette fonctionnalité est disponible depuis la section « *paramètre* ».
- La **version 2.0** intègre une **pagination** de la liste des travaux pour améliorer la lisibilité quand leur nombre devient important. L’utilisateur peut ainsi naviguer entre les pages (précédente/suivante). Il est possible de choisir le nombre d’éléments affichés par page depuis la section « *Paramètres* ».
- Nous avons ajouté la possibilité de changer l’affichage visuel entre **2 modes** :
•	*Sombre*
•	*Claire*
Le bouton permettant de choisir le mode se trouve **au coin en haut à gauche** de l’application.

# Architecture logique de l'application

À partir de la racine, voici la structure générale disponible pour **toutes les versions** :
- Dossier « */EasyLog* » : Contient le code pour gérer la création et l’écriture du fichier log et état. Le code contenu dans ce dossier est réutilisable pour toutes les versions de l’application. Ce sont des codes qui serviront uniquement de librairie et sont donc réutilisable pour d’autre parties de codes.
- Dossier « */Lib* » : Contient la partie Model qui est réutilisable pour toutes les versions de l’application. Le code concerne le langage et les travaux.
- Dossier « */CryptoSoft/CryptoEngine.cs* » : Contient le moteur de chiffrement utilisé sur les fichiers ciblés sous forme de code.
- Dossier « */CryptoSoft* » : contient l’exécutable externe de chiffrement appelé par EasySave pendant la sauvegarde.

À partir de la racine, voici la structure de la **version 1.0** :
- Dossier « */Console* » : Contient tout le code lié spécifiquement à la version 1.0 de l’application en mode console.
- Dossier « */Console/View* » : Contient tout le code concernant la partie View. Le code concerne l’affichage console.
- Dossier « *Console/ViewModel* » : Contient tout le code concernant la partie ViewModel. Le code concerne le contrôleur ainsi que les différents options (Design pattern de stratégie).

À partir de la racine, voici la structure de la **version 1.1** :
- Fichier « */Console/ViewModel/Strategy/ChangeLogFormat6.cs* » : Contient tout le code permettant de changer le format du fichier journalier.

À partir de la racine, voici la structure de la **version 2.0** :
- Dossier « */WPF* » : contient tout le code de la version graphique (interface, logique MVVM, paramètres, pagination, logiciel métier).
- Dossier « */WPF/Views* » : contient la partie affichage (fenêtre principale et boîte de dialogue de création).
- Dossier « */WPF/ViewModels* » : contient la logique applicative côté interface (commandes, exécution des sauvegardes, état, localisation).
- Dossier « */WPF/Services* » : contient les services techniques, notamment la gestion des paramètres généraux.
- Dossier « */WPF/Helpers* » : contient les helpers (convertisseurs et utilitaires de binding) utilisés par l’interface.
- Dossier « */WPF/Themes* » : contient les styles et couleurs de l’interface.

Quant à la structure de la **version 3.0**, elle sera similaire (à partir de la racine) à celle de la version 2.0 avec ces ajouts supplémentaires :
- Dossier « *LogCentral* » : contient le code pour simuler la connexion à un serveur (Docker)
- Dossier « *central-logs* » : permet de stocker le fichier journalier qui sera copié vers le serveur lors de la simulation pour la centralisation de celle-ci.


# Bonne pratique de développement appliquer

Voici les **bonnes pratiques** que nous avons utilisé pour rendre le code de l'application évolutive et maintenable :
- Paradigme de la POO appliquer sur tout le code entier
- Architecture MVVM pour toutes les versions
- Design pattern de stratégie appliquer pour la logique de choix d'option (partie ViewModel) afin d'éviter l'alourdissement des boucles `switch`
- Tests unitaires implémenter depuis la racine dans le dossier `UnitTest` pour tester les fonctionnalités de l'application et pour l'aide au débogage
- Modularité des codes pousser au maximum pour une meilleure maintenance du code

# Pipeline de développement du dépôt en ligne

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

En résumé, lorsque aucune erreur n'a été détécté après qu'un développeur partage sa branche dans le dépôt en ligne, la **pipeline** permet durant le développement qu'à chaque modification de code, la mise à jour du code suit ce chemin de versionning :
`branche du développeur` -> `production` -> `main`

Enfin, nous avons décidé de créer une **branche spécifique (v1, V1.1, v2, V3)** pour chaque version de l’application afin de garder le code de celles-ci.

# Quelque commandes utiles durant le développement

**Lignes de commandes** à utiliser depuis un **terminal** lors du développement de l'application :
```bash
dotnet build     # pour compiler le code
dotnet run     # pour démarrer l'application
dotnet test    # pour démarrer les test unitaires
dotnet test --logger "console;verbosity=detailed"    # pour démarrer les tests unitaires avec affichage de débogage
dotnet test --filter "FullyQualifiedName~Namespace.NomDeClasse.NomDeMéthode"    # pour démarrer un test unitaire en particulier
```

# Livrables divers à rendre

D'autres éléments de livrables seront aussi rendu séparément à savoir :
- un **UML (diagramme de classe + séquence)** pour chaque version de l'application
- un **manuel utilisateur (environ 3-4 pages)** à la fin du développement de l'application
