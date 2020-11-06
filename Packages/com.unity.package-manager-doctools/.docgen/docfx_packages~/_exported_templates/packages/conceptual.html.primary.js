// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.

var common = require('./common.js');
var extension = require('./conceptual.extension.js');

exports.transform = function (model) {
  if (extension && extension.preTransform) {
    model = extension.preTransform(model);
  }

  model._disableToc = model._disableToc || !model._tocPath || (model._navPath === model._tocPath);
  if (!(model.path.indexOf('api/') == 0) && !(model.path.indexOf('license/') == 0) && !model.enableTocForManual) {
    model._disableToc = true;
  }
  if ((model.path.indexOf('rt/') == 0)) {
    model._disableToc = true;
  }
  if (model.path.indexOf('manual/') == 0) {
    model._pageType = 'manual';
  }

  // If it's the homepage, redirect to manual
  model._redirectHome = false;
  if ((model.path.indexOf('index.md') == 0)) {
    model._redirectHome = true;
  }

  model.docurl = model.docurl || common.getImproveTheDocHref(model, model._gitContribute, model._gitUrlPattern);

  if (extension && extension.postTransform) {
    model = extension.postTransform(model);
  }

  return model;
}