"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/b3BotHub").build();

connection.on("ViewerCountChanged", function (followerCount) {
    var count = followerCount.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
    $('.count').html(count);
});

connection.start();
