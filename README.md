# Stellar Idle

Jeu mobile **idle / incrémental 2D** sur le thème de l'espace, fait avec Unity, pour Android (iOS pourra être ajouté plus tard).
Le joueur tape sur une planète pour gagner des crédits, achète des bâtiments qui produisent automatiquement (drones, bases lunaires, sphères de Dyson…), continue de gagner hors ligne, puis fait un *prestige* (« Warp ») pour gagner de la Matière noire qui accélère les parties suivantes.

## Boucle de jeu

| Onglet | Rôle | Remis à zéro au Warp ? |
|---|---|---|
| **Build** | Acheter des bâtiments (x1 / x10 / MAX). Production x2 à 10, 25, 50, 100… exemplaires (barre de progression). | Oui |
| **Research** | Achats uniques en crédits : x3 par bâtiment (à 10/25/50/100/200 exemplaires) et x2 global. Donne une décision toutes les quelques minutes en milieu de partie. | Oui |
| **Warp** | Prestige : gain de Matière noire = √(crédits gagnés dans la galaxie / 1M). Chaque point **gagné** donne +2 % passif, et la Matière noire se **dépense** dans 7 améliorations permanentes (production, tap, coûts, crédits de départ, hors ligne, durée du boost, capsules). | Non |
| **Goals** | Succès (bâtiments possédés, crédits gagnés, nombre de Warps) récompensés en cristaux. | Non |
| **Shop** | Achats intégrés, Time Warp (cristaux), restauration, confidentialité. | — |

Les onglets affichent une pastille quand une action est possible. Tous les chiffres sont dans `GameBalance.json`.

**Visuels :** déposer des PNG dans `Assets/Resources/Art/` (voir le `README.txt` du dossier) remplace automatiquement les formes générées.

## Monétisation (non intrusive)

| Source | Ce que reçoit le joueur | Pourquoi ce n'est pas intrusif |
|---|---|---|
| **Pub récompensée « Boost x2 »** | Production x2 pendant 4 h (cumulable jusqu'à 12 h) | C'est le joueur qui choisit de la regarder |
| **Pub récompensée « Gains hors ligne x2 »** | Double les gains accumulés pendant l'absence | Proposée seulement au retour dans le jeu, à côté d'un bouton « Collect » gratuit |
| **Pub récompensée « Capsule cargo »** | 5 min de production (min. 50 crédits) | Une capsule traverse le décor toutes les 4 à 8 min et reste 25 s ; on peut l'ignorer ; 6 par heure au maximum |
| **IAP VIP Pass** (non-consommable) | x2 permanent, toutes les récompenses sans regarder de pub, 12 h hors ligne au lieu de 4 h | C'est l'achat « Supprimer les pubs » des idle games |
| **IAP Starter Pack** (achat unique) | 300 cristaux + boost x2 de 8 h | Offre unique, à petit prix |
| **IAP Cristaux** (consommables) | Monnaie premium utilisée pour le **Time Warp** (4 h de production immédiate) | Ne bloque jamais la progression |

Il n'y a **ni bannière ni pub interstitielle**. Si une pub n'est pas disponible, le joueur reçoit quand même ses gains de base.

## Démarrer

1. Installer **Unity Hub**, puis le **dernier Unity 6 LTS** avec le module **Android Build Support** (y compris OpenJDK et Android SDK & NDK).
2. Dans Unity Hub : *Add → Add project from disk* → choisir ce dossier. Si Hub signale une version manquante, sélectionner ta version Unity 6 LTS.
3. À la première ouverture, le script `Assets/Editor/ProjectSetup.cs` crée `Assets/Scenes/Main.unity`, l'ajoute aux Build Settings et configure Android (IL2CPP, ARM64, portrait). On peut le relancer via le menu **Stellar Idle → Setup Project**.
4. Appuyer sur **Play** : le jeu complet se lance (l'interface est construite par le code, aucun prefab à configurer).
5. Faire le **premier commit** après cette ouverture, pour inclure les fichiers `.meta` générés par Unity (ne jamais committer `Library/`).

> Le fichier `Packages/manifest.json` a été écrit à la main, car aucun éditeur Unity n'était disponible lors de la génération du projet. Si Unity propose des mises à jour de paquets, tu peux les accepter.

Les pubs et les achats sont **simulés** (`MockAdsService`, `MockStoreService`) : tout fonctionne dans l'éditeur, gratuitement. Un build *release* affiche une erreur dans la console tant que les vrais SDK ne sont pas branchés.

## Architecture

```
Assets/
  Resources/GameBalance.json   ← TOUT l'équilibrage : coûts, production, durées, récompenses IAP
  Scripts/Core/                ← règles du jeu en C# pur, sans Unity, testées unitairement
    IdleEconomy*.cs            ← achat, production, paliers x2, hors ligne, boost, prestige, IAP,
                                 recherche (.Research), arbre de Matière noire (.DarkMatter), succès (.Achievements)
    GameState.cs               ← données sauvegardées (tolère l'ajout ou le retrait de bâtiments)
    NumberFormat.cs            ← 1.23K, 4.56M, 7.89aa…
    ProductIds.cs              ← identifiants des produits Google Play et des emplacements de pub
  Scripts/Runtime/
    GameManager.cs             ← cycle de vie : tick, autosave toutes les 15 s, pause et reprise, hors ligne
    SaveService.cs             ← sauvegarde JSON atomique avec backup
    Bootstrap.cs               ← lance le jeu dans n'importe quelle scène
    Services/                  ← IAdsService / IStoreService + mocks + ServiceFactory
    UI/                        ← MainScreen + un panneau par onglet (BuildingsPanel, ResearchPanel, WarpPanel,
                                 GoalsPanel, ShopPanel), ListRow réutilisable, Art (emplacements d'images), effets
  Editor/ProjectSetup.cs       ← configuration Android en un clic
  Tests/EditMode/              ← 27 tests (Window → General → Test Runner)
```

**Points clés :**
- Pour **rééquilibrer** le jeu, il suffit de modifier `GameBalance.json`, sans toucher au code. Ce fichier pourra ensuite être piloté à distance (Remote Config) pour faire de l'A/B testing.
- Pour **brancher les vrais SDK**, il suffit d'implémenter `IAdsService` et `IStoreService`, puis de les retourner dans `ServiceFactory.cs`. Le reste du jeu ne change pas.
- Les récompenses IAP sont distribuées uniquement dans `IdleEconomy.GrantProduct`, une fonction idempotente pour les achats non-consommables, ce qui garantit des restaurations d'achats correctes.
- Le temps hors ligne se base sur l'horloge UTC. Un recul de l'horloge ne rapporte rien et le temps comptabilisé est plafonné, ce qui limite la triche.

## Prochaines étapes

1. **Ouvrir le projet et jouer** pour ajuster l'équilibrage dans `GameBalance.json`.
2. ~~Pubs LevelPlay~~ : fait (`Assets/Scripts/Ads/`), pubs récompensées uniquement, ad unit `p15gaz3j25581h18`.
3. ~~Consentement RGPD / CCPA~~ : fait (popup au premier lancement, Boutique → Privacy). **Remplacer `PrivacyConsent.PrivacyPolicyUrl`** par ta vraie politique de confidentialité.
4. **Achats** : intégrer **Unity IAP** via `UnityIapStoreService : IStoreService`, et créer les produits `vip_pass`, `starter_pack`, `crystals_small` et `crystals_large` dans la Google Play Console.
5. **Visuels et sons** : remplacer les sprites générés (`ProceduralSprites.cs`) par de vrais assets.
6. **Analytics** : Unity Analytics ou Firebase, pour suivre la rétention J1/J7 et le taux de visionnage des pubs.
7. **Publication** : créer un compte Google Play Console (25 $ une seule fois), une clé de signature, un build `.aab`, puis passer par un test fermé (12 testeurs pendant 14 jours pour les nouveaux comptes personnels) avant la production.
