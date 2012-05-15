using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using JabbR.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace JabbR.Handlers
{
    public class MessagesHandler : ApiHandlerBase
    {
        const string FilenameDateFormat = "yyyy-MM-dd.HHmmsszz";

        IJabbrRepository _repository;

        public MessagesHandler(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public override void Process()
        {
            var request = Context.Request;
            var routeData = request.RequestContext.RouteData.Values;

            var roomName = (string)routeData["room"];
            var range = request["range"];

            if (String.IsNullOrWhiteSpace(range))
            {
                range = "last-hour";
            }

            var end = DateTime.Now;
            DateTime start;

            switch (range)
            {
                case "last-hour":
                    start = end.AddHours(-1);
                    break;
                case "last-day":
                    start = end.AddDays(-1);
                    break;
                case "last-week":
                    start = end.AddDays(-7);
                    break;
                case "last-month":
                    start = end.AddDays(-30);
                    break;
                case "all":
                    start = DateTime.MinValue;
                    break;
                default:
                    Writer.WriteBadRequest("range value not recognized");
                    return;
            }

            ChatRoom room = null;

            try
            {
                room = _repository.VerifyRoom(roomName, mustBeOpen: false);
            }
            catch (Exception ex)
            {
                Writer.WriteNotFound(ex.Message);
                return;
            }

            if (room.Private)
            {
                // TODO: Allow viewing messages using auth token
                Writer.WriteNotFound(String.Format("Unable to locate room {0}.", room.Name));
                return;
            }

            var messages = _repository.GetMessagesByRoom(room)
                .Where(msg => msg.When <= end && msg.When >= start)
                //.OrderBy(msg => msg.When)
                .Select(msg => new
                {
                    Content = msg.Content,
                    Username = msg.User.Name,
                    When = msg.When
                });

            var filenamePrefix = roomName + ".";

            if (start != DateTime.MinValue)
            {
                filenamePrefix += start.ToString(FilenameDateFormat, CultureInfo.InvariantCulture) + ".";
            }

            filenamePrefix += end.ToString(FilenameDateFormat, CultureInfo.InvariantCulture);

            Writer.WriteResponseObejct(messages, filenamePrefix);
        }
    }
}