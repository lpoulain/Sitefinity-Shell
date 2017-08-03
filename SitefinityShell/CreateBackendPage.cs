using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Localization;
using Telerik.Sitefinity.Model.Localization;
using Telerik.Sitefinity.Modules.GenericContent.Web.UI;
using Telerik.Sitefinity.Modules.Pages;
using Telerik.Sitefinity.Pages.Model;
using Telerik.Sitefinity.Web.UI;
using Telerik.Sitefinity.Web.UI.PublicControls;

namespace SitefinityShell
{
	class CreateBackendPage
	{
		public static void Create()
		{
			PageManager pageMgr = PageManager.GetManager();
			pageMgr.Provider.SuppressSecurityChecks = true;

			PageNode shellPage = pageMgr.GetPageNodes().Where(page => page.RootNodeId == SiteInitializer.BackendRootNodeId && page.Title == "Shell" && !page.IsDeleted).FirstOrDefault();
			if (shellPage == null)
			{
				var pageId = Guid.NewGuid();
				PageNode parent = pageMgr.GetPageNodes().Where(page => page.RootNodeId == SiteInitializer.BackendRootNodeId && page.Title == "$Resources: PageResources,ToolsNodeTitle").FirstOrDefault();
				if (parent == null)
					parent = pageMgr.GetPageNodes().Where(page => page.RootNodeId == SiteInitializer.BackendRootNodeId && page.Title == "Sitefinity").FirstOrDefault();

				PageTemplate template = pageMgr.GetTemplates().Where(t => t.Title == "Default Backend Template").FirstOrDefault();

				PageNode pageNode = pageMgr.CreatePage(parent, pageId, NodeType.Standard);
				PageData pageData = pageNode.GetPageData();

				pageData.Template = template;
				pageData.Culture = Thread.CurrentThread.CurrentCulture.ToString();
				String pageName = "Shell";
				pageData.HtmlTitle = pageName;
				pageNode.Name = pageName;
				pageNode.Description = pageName;
				pageNode.Title = pageName;
				pageNode.UrlName = "Shell";
				pageNode.ShowInNavigation = true;
				pageNode.DateCreated = DateTime.UtcNow;
				pageNode.LastModified = DateTime.UtcNow;
				pageNode.ApprovalWorkflowState = "Published";

				PageDraft draft = pageMgr.EditPage(pageData.Id);

				// Add layout
//				var contentPlaceholder = GetContentPlaceholderId(pageData);
//				var contentCaption = "LayoutControlInContentPlaceholder";
//				AddLayoutControlToPage(pageId, contentPlaceholder, contentCaption, "");

				// 
				var ctrl_css = new CssEmbedControl();
				ctrl_css.CustomCssCode = @"
	div.console {
		word-wrap: break-word;
	}
	div.console {
		padding-left: 10px;
		font-size: 14px;
		margin-top: 1em;
	}

		div.console div.jquery-console-inner {
			width: 1200px;
			height: 800px;
			background: #efefef;
			padding: 0.5em;
			overflow: auto;
		}

		div.console div.jquery-console-prompt-box {
			color: #444;
			font-family: monospace;
		}

		div.console div.jquery-console-focus span.jquery-console-cursor {
			background: #333;
			color: #eee;
			font-weight: bold;
		}

		div.console div.jquery-console-message-error {
			color: #ef0505;
			font-family: sans-serif;
			font-weight: bold;
			padding: 0.1em;
		}

		div.console div.jquery-console-message-success {
			color: #187718;
			font-family: monospace;
			padding: 0.1em;
		}

		div.console span.jquery-console-prompt-label {
			font-weight: bold;
		}
					";
				var pageControl = pageMgr.CreateControl<PageDraftControl>(ctrl_css, "Content");
				pageControl.Caption = "Style";
				pageMgr.SetControlDefaultPermissions(pageControl);
				draft.Controls.Add(pageControl);

				var ctrl_js0 = new JavaScriptEmbedControl();
				ctrl_js0.Url = "https://code.jquery.com/jquery-2.1.1.min.js";
				pageControl = pageMgr.CreateControl<PageDraftControl>(ctrl_js0, "Content");
				pageControl.Caption = "JavaScript 0";
				pageMgr.SetControlDefaultPermissions(pageControl);
				draft.Controls.Add(pageControl);

				var ctrl_js2 = new JavaScriptEmbedControl();
				ctrl_js2.ScriptEmbedPosition = Telerik.Sitefinity.Web.UI.PublicControls.Enums.ScriptEmbedPosition.InPlace;
				ctrl_js2.CustomJavaScriptCode = @"
	var list = '';
	var root = '';
	var resource = 'pages';
	var site = '';
	var provider = '';
	var controller;
	$(document).ready(function () {
		var console = $('.console');
		controller = console.console({
			promptLabel: 'Pages> ',
			autofocus: true,
			completeHandle: function (promptText) {
				promptText = promptText.trim();
				if (promptText == '') return;
				var words = promptText.split(' ');
				var keyword = words[words.length - 1];
				var len = keyword.length;
				return list.filter(function (elt) { return elt.toLowerCase().startsWith(keyword.toLowerCase()); })
						   .map(function (elt) { return elt.substring(len, 36); });
			},
			cols: 80,
			commandValidate: function (line) {
				if (line == '') return false;
				else return true;
			},
			commandHandle: function (line, report) {
				$.ajax({
					url: '/Sitefinity/Services/ShellModule/ShellService.svc/Command', data: { root: root, rsc: resource, cmd: line, site: site, provider:provider },
					success: function (data) {
						if (data.response == undefined && data.indexOf('Temporarily unavailable due to maintenance') >= 0) {
							report([{ msg: 'Site is being restarted', className: 'jquery-console-message-error' }]);
							return;
						}
						if (data.response != '') list = data.response.split('\n');
						if (data.path != '') controller.promptLabel = data.path;
						if (data.root != '') root = data.root;
						if (data.resource != '') resource = data.resource;
						if (data.site != '') site = data.site;
						provider = data.provider;
						if (data.error != '') report([{ msg: data.error, className: 'jquery-console-message-error' }]);
						if (data.response != '' || data.error == '') report([{ msg: data.response, className: 'jquery-console-message-success' }]);
					},
					error: function (xhr, ajaxOptions, thrownError) {
						report([{ msg: 'Error: ' + xhr.status + '\n' + thrownError, className: 'jquery-console-message-error' }]);
					}
				});
			},
			animateScroll: true,
			promptHistory: true,
		});
	});
				";
				pageControl = pageMgr.CreateControl<PageDraftControl>(ctrl_js2, "Content");
				pageControl.Caption = "JavaScript 2";
				pageMgr.SetControlDefaultPermissions(pageControl);
				draft.Controls.Add(pageControl);

				// 
				var ctrl_js1 = new JavaScriptEmbedControl();
				ctrl_js1.CustomJavaScriptCode = @"!function(e){var n=!!~navigator.userAgent.indexOf("" AppleWebKit/"");e.fn.console=function(o){function t(){N=0,X="""",F=0,v(),O=e('<div class=""jquery-console-prompt-box""></div>');var n=e('<span class=""jquery-console-prompt-label""></span>'),o=U.continuedPrompt?B:U.promptLabel;O.append(n.text(o).show()),n.html(n.html().replace("" "",""&nbsp;"")),P=e('<span class=""jquery-console-prompt""></span>'),O.append(P),R.append(O),H()}function r(e){return(e.keyCode==L.tab||192==e.keyCode)&&e.altKey}function s(e){if(0!=E.length){(F+=e)<0?F=E.length:F>E.length&&(F=0);X=0==F?Y:E[F-1],o.historyPreserveColumn?X.length<N+1?N=X.length:0==N&&(N=X.length):N=X.length,H()}}function c(){s(-1)}function i(){s(1)}function a(e){E.push(e),Y=""""}function l(){return N<X.length&&(X=X.substring(0,N)+X.substring(N+1),Y=X,!0)}function u(){l()&&H()}function f(){R.children("".jquery-console-prompt-box, .jquery-console-message"").remove(),U.report("" ""),U.promptText(""""),U.focus()}function p(){var e=jQuery.fn.jquery.split("".""),n=parseInt(e[0]),o=parseInt(e[1]);1==n&&o>6||n>1?R.prop({scrollTop:R.prop(""scrollHeight"")}):R.attr({scrollTop:R.attr(""scrollHeight"")})}function d(){""function""==typeof o.cancelHandle&&o.cancelHandle()}function m(){if(""function""==typeof o.commandHandle){h(),a(X);var e=X;U.continuedPrompt?Z?Z+=""\n""+X:Z=X:Z=void 0,Z&&(e=Z);var n=o.commandHandle(e,function(e){g(e)});U.continuedPrompt&&!Z&&(Z=X),""boolean""==typeof n?n?g():g(""Command failed."",""jquery-console-message-error""):""string""==typeof n?g(n,""jquery-console-message-success""):""object""==typeof n&&n.length?g(n):U.continuedPrompt&&g()}}function h(){J=!1}function v(){J=!0}function g(n,o){if(N=-1,H(),""string""==typeof n)b(n,o);else if(e.isArray(n))for(var r in n){var s=n[r];b(s.msg,s.className)}else R.append(n);t()}function y(e,n){var o=X;O.remove(),g(e,n),U.promptText(o)}function b(n,o){var t=e('<div class=""jquery-console-message""></div>');o&&t.addClass(o),t.filledText(n).hide(),R.append(t),t.show()}function C(e){return N+e>=0&&N+e<=X.length&&(N+=e,!0)}function j(){return!!C(1)&&(H(),!0)}function x(){return!!C(-1)&&(H(),!0)}function q(){C(-N)&&H()}function T(){C(X.length-N)&&H()}function w(e){if(""string""==typeof e){var n=e.charCodeAt();return n>=""A"".charCodeAt()&&n<=""Z"".charCodeAt()||n>=""a"".charCodeAt()&&n<=""z"".charCodeAt()||n>=""0"".charCodeAt()&&n<=""9"".charCodeAt()}return!1}function A(){if(""function""==typeof o.completeHandle){var e=o.completeHandle(X),n=e.length;if(1===n)U.promptText(X+e[0]);else if(n>1&&o.cols){for(var t=X,r=0,s=0;s<n;s++)r=Math.max(r,e[s].length);r+=2;var c=Math.floor(o.cols/r),i="""",a=0;for(s=0;s<n;s++){var l=e[s];i+=e[s];for(var u=l.length;u<r;u++)i+="" "";++a>=c&&(i+=""\n"",a=0)}g(i,""jquery-console-message-value""),U.promptText(t)}}}function I(){""function""==typeof o.completeIssuer&&o.completeIssuer(X)}function k(e,n){var t=n.length;if(1===t)U.promptText(e+n[0]);else if(t>1&&o.cols){for(var r=e,s=0,c=0;c<t;c++)s=Math.max(s,n[c].length);s+=2;var i=Math.floor(o.cols/s),a="""",l=0;for(c=0;c<t;c++){var u=n[c];a+=n[c];for(var f=u.length;f<s;f++)a+="" "";++l>=i&&(a+=""\n"",l=0)}g(a,""jquery-console-message-value""),U.promptText(r)}}function H(){var e=X,n="""";if(N>0&&""""==e)n=z;else if(N==X.length)n=K(e)+z;else{var o=e.substring(0,N),t=e.substring(N,N+1);t&&(t='<span class=""jquery-console-cursor"">'+K(t)+""</span>"");var r=e.substring(N+1);n=K(o)+t+K(r)}P.html(n),p()}function K(e){return e.replace(/&/g,""&amp;"").replace(/</g,""&lt;"").replace(/</g,""&lt;"").replace(/ /g,""&nbsp;"").replace(/\n/g,""<br />"")}var L={37:x,39:j,38:c,40:i,8:function(){C(-1)&&(l(),H())},46:u,35:T,36:q,13:function(){var e=X;if(""function""==typeof o.commandValidate){var n=o.commandValidate(e);1==n||0==n?n&&m():g(n,""jquery-console-message-error"")}else m()},18:function(){},9:function(){""function""==typeof o.completeHandle?A():I()}},M={65:q,69:T,68:u,78:i,80:c,66:x,70:j,75:function(){for(;l();)H()},76:f,85:function(){U.promptText("""")}};o.ctrlCodes&&e.extend(M,o.ctrlCodes);var O,P,S={70:function(){for(;N<X.length&&!w(X[N])&&j(););for(;N<X.length&&w(X[N])&&j(););},66:function(){for(;N-1>=0&&!w(X[N-1])&&x(););for(;N-1>=0&&w(X[N-1])&&x(););},68:function(){for(;N<X.length&&!w(X[N]);)l(),H();for(;N<X.length&&w(X[N]);)l(),H()}},W={13:function(){var e=""\n""+X.split(""\n"").slice(-1)[0].match(/^(\s*)/g)[0];X+=e,C(e.length),H()}},z='<span class=""jquery-console-cursor"">&nbsp;</span>',Q=e(this),R=e('<div class=""jquery-console-inner""></div>'),V=e('<textarea autocomplete=""off"" autocorrect=""off"" autocapitalize=""off"" spellcheck=""false"" class=""jquery-console-typer""></textarea>'),B=o&&o.continuedPromptLabel?o.continuedPromptLabel:""> "",N=0,X="""",Y="""",Z="""",D=void 0===o.fadeOnReset||o.fadeOnReset,E=[],F=0,G=0,J=!0,U={};U.promptLabel=o&&o.promptLabel?o.promptLabel:""> "",Q.append(R),R.append(V),V.css({position:""absolute"",top:0,left:""-9999px""}),o.welcomeMessage&&b(o.welcomeMessage,""jquery-console-welcome""),t(),o.autofocus&&(R.addClass(""jquery-console-focus""),V.focus(),setTimeout(function(){R.addClass(""jquery-console-focus""),V.focus()},100)),U.inner=R,U.typer=V,U.scrollToBottom=p,U.report=y,U.showCompletion=k,U.clearScreen=f,U.reset=function(){var n=void 0!==o.welcomeMessage,r=function(){R.find(""div"").each(function(){n?n=!1:e(this).remove()})};D?R.parent().fadeOut(function(){r(),t(),R.parent().fadeIn($)}):(r(),t(),$())};var $=function(){R.addClass(""jquery-console-focus""),V.focus()};return U.focus=function(){$()},U.notice=function(n,o){var t=e('<div class=""notice""></div>').append(e(""<div></div>"").text(n)).css({visibility:""hidden""});Q.append(t);var r=!0;if(""fadeout""==o)setTimeout(function(){t.fadeOut(function(){t.remove()})},4e3);else if(""prompt""==o){var s=e('<br/><div class=""action""><a href=""javascript:"">OK</a><div class=""clear""></div></div>');t.append(s),r=!1,s.click(function(){t.fadeOut(function(){t.remove(),R.css({opacity:1})})})}var c=t.height();return t.css({height:""0px"",visibility:""visible""}).animate({height:c+""px""},function(){r||R.css({opacity:.5})}),t.css(""cursor"",""default""),t},Q.click(function(){return!window.getSelection().toString()&&(R.addClass(""jquery-console-focus""),R.removeClass(""jquery-console-nofocus""),n?V.focusWithoutScrolling():V.css(""position"",""fixed"").focus(),p(),!1)}),V.blur(function(){R.removeClass(""jquery-console-focus""),R.addClass(""jquery-console-nofocus"")}),V.bind(""paste"",function(e){V.val(""""),setTimeout(function(){V.consoleInsert(V.val()),V.val("""")},0)}),V.keydown(function(e){G=0;var n=e.keyCode;if(e.ctrlKey&&67==n)return G=n,d(),!1;if(J){if(e.shiftKey&&n in W)return G=n,W[n](),!1;if(e.altKey&&n in S)return G=n,S[n](),!1;if(e.ctrlKey&&n in M)return G=n,M[n](),!1;if(n in L)return G=n,L[n](),!1}}),V.keypress(function(e){var t=e.keyCode||e.which;if(r(e))return!1;if((e.ctrlKey||e.metaKey)&&""v""==String.fromCharCode(t).toLowerCase())return!0;if(J&&G!=t&&t>=32){if(G)return!1;(void 0===o.charInsertTrigger||""function""==typeof o.charInsertTrigger&&o.charInsertTrigger(t,X))&&V.consoleInsert(t)}return!n&&void 0}),V.consoleInsert=function(e){var n=""number""==typeof e?String.fromCharCode(e):e,o=X.substring(0,N),t=X.substring(N);X=o+n+t,C(n.length),Y=X,H()},U.promptText=function(e){return""string""==typeof e&&(N=(X=e).length,H()),X},U},e.fn.filledText=function(n){return e(this).text(n),e(this).html(e(this).html().replace(/\t/g,""&nbsp;&nbsp;"").replace(/\n/g,""<br/>"")),this},e.fn.focusWithoutScrolling=function(){var n=window.scrollX,o=window.scrollY;e(this).focus(),window.scrollTo(n,o)}}(jQuery);";
				ctrl_js1.ScriptEmbedPosition = Telerik.Sitefinity.Web.UI.PublicControls.Enums.ScriptEmbedPosition.InPlace;
				pageControl = pageMgr.CreateControl<PageDraftControl>(ctrl_js1, "Content");
				pageControl.Caption = "JavaScript 1";
				pageMgr.SetControlDefaultPermissions(pageControl);
				draft.Controls.Add(pageControl);

				// Add content block
				var cb = new ContentBlock();
				cb.Html = "<div class=\"console\"></div><br><br>";
				pageControl = pageMgr.CreateControl<PageDraftControl>(cb, "Content");
				pageControl.Caption = "Shell";
				pageMgr.SetControlDefaultPermissions(pageControl);
				draft.Controls.Add(pageControl);

				// Saves the page
				var master = pageMgr.PagesLifecycle.CheckIn(draft);
				pageMgr.PagesLifecycle.Publish(master);

				pageMgr.SaveChanges();		

			}
		}

		public static string GetContentPlaceholderId(PageData pageData)
		{
			var contentAndSideBar = pageData.Template.Controls.FirstOrDefault(); // (x => x.Caption.Contains("Content"));

			// You can specify which column to get ( e.g. Content & Right Sidebar)
			// using the PlaceHolders collection from the control itself.
			var contentPlaceHolder = contentAndSideBar.PlaceHolders[0];
			var rigthSideBarPlaceHolder = contentAndSideBar.PlaceHolders[1];
			return contentPlaceHolder;
		}

		public static PageDraftControl CreateLayoutControl(PageManager manager, string placeHolder, string caption, string layoutResource)
		{
			var pageControl = manager.CreateControl<PageDraftControl>();
			pageControl.Caption = caption;
			pageControl.ObjectType = typeof(LayoutControl).FullName;

			pageControl.PlaceHolder = placeHolder;
			pageControl.IsLayoutControl = true;
			manager.SetControlDefaultPermissions(pageControl);

/*			var prop = manager.CreateProperty();
			prop.Name = "Layout";
			prop.Value = layoutResource;
			pageControl.Properties.Add(prop);*/

			return pageControl;
		}

		public static void AddLayoutControlToPage(Guid pageId, string placeholder, string caption, string layoutResource)
		{
			using (new CultureRegion(LocalizationHelper.GetSitefinityCulture(null)))
			{
				var manager = PageManager.GetManager();
				var pageData = manager.GetPageNode(pageId).GetPageData();

				var temp = manager.EditPage(pageData.Id);
				var layoutControl = CreateLayoutControl(manager, placeholder, caption, layoutResource);

				manager.SetControlId(temp, layoutControl);

				// Add the control to the page
				temp.Controls.Add(layoutControl);

				var master = manager.PagesLifecycle.CheckIn(temp);
				manager.PagesLifecycle.Publish(master);
				manager.SaveChanges();
			}
		}
	}
}
