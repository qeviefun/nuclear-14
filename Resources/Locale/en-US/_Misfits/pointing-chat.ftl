## Misfits Chat Action Broadcasting — #Misfits Add
# Emote chat text broadcast for player interactions that normally only show as sprite popups.
# All strings are the action portion only; the emote system wraps them as: "* <name> <message> *"

## PointingChatSystem
pointing-chat-point-at-other = points at {$other}

## OfferItemSystem
# Broadcast when a player hands an item to another player after accepting an offer.
misfits-chat-offer-handoff = hands {$item} to {$target}

## CarryingSystem
# Broadcast when a player picks up or puts down another entity.
misfits-chat-carry-pickup = picks up {$carried}
misfits-chat-carry-drop = puts down {$carried}
misfits-chat-carry-throw = hurls {$victim}

## Knockdown Systems
misfits-chat-knockdown-hit = knocks {$target} to the ground
misfits-chat-knockdown-hit-victim = is knocked to the ground by {$user}
misfits-chat-collision-knockdown = slams {$target} sprawling
misfits-chat-collision-knockdown-victim = is sent sprawling by {$user}

## CPRSystem
misfits-chat-cpr-start = begins giving CPR to {$target}
misfits-chat-cpr-victim = is being given CPR by {$user}

## FoodSystem / DrinkSystem
misfits-chat-force-feed-start = tries to force-feed {$target} with {$item}
misfits-chat-force-feed-victim = is being force-fed {$item} by {$user}
misfits-chat-force-drink-start = tries to force {$target} to drink {$item}
misfits-chat-force-drink-victim = is being forced to drink {$item} by {$user}

## BuckleChatSystem
misfits-chat-buckle-strap = straps {$target} onto {$strap}
misfits-chat-buckle-victim = is strapped onto {$strap} by {$user}
misfits-chat-unbuckle-release = frees {$target} from {$strap}
misfits-chat-unbuckle-victim = is freed from {$strap} by {$user}

## StrippableSystem
misfits-chat-strip-remove = strips {$item} off {$target}
misfits-chat-strip-victim-remove = has {$item} stripped off by {$user}
misfits-chat-gag-apply = gags {$target} with {$item}
misfits-chat-gag-victim = is gagged with {$item} by {$user}

## EscapeInventorySystem
misfits-chat-struggle-carried = struggles in {$carrier}'s grip

## ResistLockerSystem
misfits-chat-locker-struggle = pounds against {$container} from the inside
misfits-chat-locker-breakout = kicks their way out of {$container}

## SurgerySystem
misfits-chat-surgery-start = begins operating on {$target}
misfits-chat-surgery-victim = is being operated on by {$user}

## BodySystem
misfits-chat-gib-body = bursts apart in a shower of gore
misfits-chat-gib-part = loses their {$part}

## ThrowImpactChatSystem
misfits-chat-throw-hit = hits {$target} with {$item}
misfits-chat-throw-hit-victim = is hit by {$user}'s {$item}
misfits-chat-throw-poison-hit = poisons {$target} with {$item}
misfits-chat-throw-poison-hit-victim = is poisoned by {$user}'s {$item}

## CuffingChatSystem
# Broadcast when a player begins trying to restrain another entity with handcuffs.
misfits-chat-cuff-start = tries to restrain {$target}
misfits-chat-cuff-start-self = tries to restrain themselves
# Broadcast when a player successfully restrains another entity with handcuffs.
misfits-chat-cuff-applied = restrains {$target}
misfits-chat-cuff-self = restrains themselves
# Broadcast when a player begins trying to remove cuffs from another entity.
misfits-chat-uncuff-start = tries to remove restraints from {$target}
misfits-chat-uncuff-start-self = tries to remove their own restraints
## GrabChatSystem
# Broadcast when a mob entity is grabbed/pulled by another entity.
misfits-chat-grab-start = grabs {$grabbed}
misfits-chat-double-grab-cinch = cinches {$victim} into a chokehold
misfits-chat-double-grab-victim = is being chokeholded by {$carrier}
misfits-chat-double-grab-resist = struggles against {$carrier}'s grip
misfits-chat-double-grab-throw = hurls {$victim}
misfits-chat-double-grab-gasp = gasps for air
misfits-chat-inject-other = injects {$target} with {$item}

## BlockingChatSystem
# Broadcast when a player raises their shield to block.
misfits-chat-blocking-start = raises their {$shield}
# Broadcast when a player lowers their shield.
misfits-chat-blocking-stop = lowers their {$shield}

## DisarmChatSystem
# Broadcast when a player successfully disarms and knocks another player down (stam-crit).
misfits-chat-disarm-knockdown = pushes {$target} down
