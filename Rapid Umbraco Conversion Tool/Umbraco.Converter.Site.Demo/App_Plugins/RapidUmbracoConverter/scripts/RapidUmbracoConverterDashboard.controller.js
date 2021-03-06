﻿angular.module("umbraco").controller("RapidUmbracoConverterDashboardController", function ($scope, userService, $http, umbRequestHelper) {
    var vm = this;

    vm.TemplatePath = "~/react-demo";
    vm.UserName = "";
    vm.Response = "";
    vm.GeneratedDocumentTypes = [];
    vm.GeneratedDocumentTypeNumber = 0;

    vm.FoundTemplateFiles = [];


    vm.FileCopyPairCollection = [
        {
            VirtualPath: "~/React/Stuff",
            Source: "/Stuff",
            MarkupReference: "/MFR",
            DestinationFolder: "/STUFF"
        }
    ];

    vm.NewFileCopyPairSource = {
        VirtualPath: "",
        Source: "",
        MarkupReference: "",
        DestinationFolder: ""
    };


    vm.FocusValue = function (fileItem) {
        vm.NewFileCopyPairSource.VirtualPath = fileItem.VirtualPath
        vm.NewFileCopyPairSource.Source = fileItem.Source;
        vm.NewFileCopyPairSource.MarkupReference = fileItem.MarkupReference;
        vm.NewFileCopyPairSource.DestinationFolder = fileItem.DestinationFolder;
        ;
    };

    vm.RemoveItem = function (fileItem) {
        vm.FileCopyPairCollection.splice(vm.FileCopyPairCollection.indexOf(fileItem), 1);
    };

    vm.AddNewFileCopyPair = function () {
        vm.FileCopyPairCollection.push(JSON.parse(JSON.stringify(vm.NewFileCopyPairSource)));

        vm.NewFileCopyPairSource.VirtualPath;
        vm.NewFileCopyPairSource.Source = "";
        vm.NewFileCopyPairSource.MarkupReference = "";
        vm.NewFileCopyPairSource.DestinationFolder = "";

        $("#newSource").focus();


    };


    userService.getCurrentUser().then(function (user) {
        console.log(user);
        vm.UserName = user.name;
    });


    vm.RefreshTemplatePath = function () {
        //Go to the services, get the index files


        var postData = {};
        postData.TemplateDirectory = vm.TemplatePath;


        $.ajax({
            type: "POST",
            url: "/umbraco/backoffice/RapidUmbracoConverter/Converter/GetConvertableFiles",
            contentType: "application/json; charsect=utf-8",
            dataType: "json",
            data: JSON.stringify(postData)
        }).then(function (data) {
            vm.FoundTemplateFiles = data
        }, function () {

        });



    },

        vm.BeginConvert = function () {

            if (vm.TemplatePath == '') {
                alert("Please enter a valid template directory");
                $("#templatePath").focus();
            }
            else {

                vm.Response = "Conversion Started";

                var data = {};
                data.TemplateDirectory = vm.TemplatePath;
                data.CopyPairCollection = vm.FileCopyPairCollection;

                $.ajax({
                    type: "POST",
                    url: "/umbraco/backoffice/RapidUmbracoConverter/Converter/BeginConvert",
                    contentType: "application/json; charsect=utf-8",
                    dataType: "json",
                    data: JSON.stringify(data)
                }).then(function (response) {
                    vm.Response = response.Message;
                    vm.GeneratedDocumentTypes = response.ContentTypes;
                    vm.GeneratedDocumentTypeNumber = response.ContentTypes.length;

                    alert("Post Complete");

                }, function () {
                    alert("There was an error");
                });
            }
        }
});