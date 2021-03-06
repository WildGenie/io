﻿"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/IO").build();

connection.on("ReceiveNewFollower", function (follower) {

    var div = document.createElement("div");
    var id = +(new Date());

    div.classList.add('newFollower');

    div.innerText = follower.displayName + ' just followed.';

    $("#container").append(div);
});

connection.on("ReceiveNewCheer", function (bitReceived) {

    var div = document.createElement("div");
    var id = +(new Date());

    div.innerText = bitReceived.username + ' just cheered ' + bitReceived.bitsUsed + '.';

    $("#container").append(div);
});

connection.on("ReceiveNewSubscription", function (subscription) {

    var div = document.createElement("div");
    var id = +(new Date());

    var name = subscription.recipientDisplayName === undefined ? subscription.displayName : subscription.recipientDisplayName;

    div.innerText = name + ' just subscribed.';

    $("#container").append(div);
});

connection.onclose(async () => {
    console.log('Closing (Alerts)');
    await start();
});

connection.start();

async function start() {
    try {
        console.log('Reconnecting (Alerts)');
        await connection.start();
    } catch (err) {
        setTimeout(() => start(), 5000);
    }
}

