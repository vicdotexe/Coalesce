﻿/// <reference path="viewmodels.generated.d.ts" />

var model = new ViewModels.Person();
model.load(1);
model.isSavingAutomatically = false;
ko.applyBindings(model);