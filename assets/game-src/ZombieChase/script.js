setup.zombieDist = 20022
setup.zombieAmount = 100
setup.multi = 1
setup.gameOver = false

window.GameData = {
	"extras": {
		"Stretch Your Arms": { "reward": -10, "type": "killZombies" },
		"Stretch Your Legs": { "reward": 1, "type": "multi" },
		"Do 10 Jumping Jacks": { "reward": -100, "type": "killZombies" }
	}
}
setup.setMode = function setMode(mode) {
	setup.mode = mode
	setup.multi = 1
}
setup.decZombieDist = function decZombieDist(amount) {
	setup.zombieDist += amount
	if (setup.zombieAmount > 0 && setup.zombieDist > 0) {
		setup.zombieDistText.textContent = `The zombies are ${setup.zombieDist} steps away!`
	} else {
		setup.clearID()
	}
	setup.checkWinLoss()

}

setup.incZombieDist = function incZombieDist(amount) {
	setup.zombieDist += amount * setup.multi
	if (setup.zombieAmount > 0 && setup.zombieDist > 0) {
		setup.zombieDistText.textContent = `The zombies are ${setup.zombieDist} steps away!`
	}

}

setup.clearID = function clearID() {
	clearInterval(setup.intervalId)
}
setup.redeem = function redeem(object, button) {
	if (button.id != "redeemed") {
		button.id = "redeemed"
		button.textContent = "Completed!"
	} else { return }

	switch (object.type) {
		case "killZombies":
			setup.zombieAmount += object.reward
			if (setup.zombieAmount > 0) {
				setup.zombieCntText.textContent = `There are ${setup.zombieAmount} left...`
			}
			break;
		case "multi":
			setup.multi += object.reward
			setup.multiText.textContent = setup.multi
			break;
	}

	setup.checkWinLoss()

}


setup.checkWinLoss = function checkWinLoss() {
	if (setup.gameOver == true) {
		return
	}
	if (setup.zombieAmount <= 0) {
		setup.zombieDistText.textContent = "All the zombies are Gone!"
		setup.zombieCntText.textContent = `There are 0 left...`
		setup.clearID()
		setup.gameOver = true;
		setTimeout(function () { Engine.play("You Win") }, 3000);
	}

	if (setup.zombieDist <= 0) {
		setup.zombieDistText.textContent = "The zombies caught you..."
		setup.zombieCntText.textContent = ""
		setup.clearID()
		setup.gameOver = true;
		setTimeout(function () { Engine.play("You Lose") }, 3000);
	}
}

setup.startChase = function startChase() {
	setup.zombieDistText = document.getElementById("zombieDist")
	setup.zombieCntText = document.getElementById("zombieCnt")
	setup.multiText = document.getElementById("multi")
	setup.decZombieDist(0)
	setup.zombieCntText.textContent = `There are ${setup.zombieAmount} left...`
	setup.multiText.textContent = setup.multi

	switch (setup.mode) {
		case "easy":
			setup.intervalId = setInterval(setup.decZombieDist, 1000, -5);
			break;
		case "med":
			setup.intervalId = setInterval(setup.decZombieDist, 1000, -10);
			break;
		case "hard":
			setup.intervalId = setInterval(setup.decZombieDist, 1000, -50);
			break;
		case "impossible":
			setup.intervalId = setInterval(setup.decZombieDist, 100, -1000);
			break;
	}

	const allKeys = Object.keys(window.GameData["extras"])
	let container = document.getElementById("extras");

	for (let i = 0; i < 3; i++) {
		let randomIndex = Math.floor(Math.random() * allKeys.length);
		let randomKey = allKeys.pop(randomIndex)
		let randomValue = window.GameData["extras"][randomKey];

		let newDiv = document.createElement('div');
		newDiv.classList.add("extra");

		let newTask = document.createElement('span')
		newTask.textContent = randomKey;
		newDiv.appendChild(newTask);

		let newButton = document.createElement('button');
		newButton.classList.add("extraButton");
		newButton.id = "notRedeemed"

		switch (randomValue.type) {
			case "killZombies":
				newButton.textContent = `Kill ${randomValue.reward} zombies`
				break;
			case "multi":
				newButton.textContent = `Increase Step Power by ${randomValue.reward}`
				break;
		}

		newButton.addEventListener('click', () => setup.redeem(randomValue, newButton));
		newDiv.appendChild(newButton);

		container.appendChild(newDiv);
	}

}

setup.reset = function reset() {
	setup.zombieAmount = 100
	setup.zombieDist = 20000
	setup.gameOver = false
}

/*
Useful later:
	newButton.addEventListener('click', () => setup.respond(name, newButton));
*/