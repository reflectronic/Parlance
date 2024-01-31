using Microsoft.EntityFrameworkCore;
using Parlance.CldrData;
using Parlance.Database;
using Parlance.Database.Models;
using Parlance.Notifications.Channels;
using Parlance.Notifications.Channels.TranslationFreeze;
using Parlance.Notifications.Email;
using Parlance.Vicr123Accounts.Services;

namespace Parlance.Notifications.Service;

public class NotificationService(ParlanceContext dbContext, IVicr123AccountsService accountsService) : INotificationService
{
    public async Task SetUnsubscriptionState(ulong userId, bool unsubscribed)
    {
        // Check if user unsubscription exists
        var unsubscription = await dbContext.NotificationUnsubscriptions.FirstOrDefaultAsync(nu => nu.UserId == userId);

        // If unsubscription exists and we want to subscribe the user, remove the unsubscription
        if (unsubscription != null && !unsubscribed)
        {
            dbContext.NotificationUnsubscriptions.Remove(unsubscription);
            await dbContext.SaveChangesAsync();
        }
        
        // If no unsubscription exists and we want to unsubscribe the user, add a new unsubscription
        else if (unsubscription == null && unsubscribed)
        {
            dbContext.NotificationUnsubscriptions.Add(new NotificationUnsubscription
            {
                UserId = userId,
            });
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> GetUnsubscriptionState(ulong userId)
    {
        // Check if user unsubscription exists
        var unsubscription = await dbContext.NotificationUnsubscriptions.FirstOrDefaultAsync(nu => nu.UserId == userId);

        // Return true if an unsubscription exists, false otherwise
        return unsubscription != null;
    }

    public async Task AddSubscriptionPreference<T>(INotificationChannelSubscription<T> subscription, bool enabled) where T : INotificationChannelSubscription<T>
    {
        dbContext.NotificationSubscriptions.Add(new NotificationSubscription()
        {
            UserId = subscription.UserId,
            Enabled = enabled,
            AutoSubscriptionSource = subscription.AutoSubscriptionSource,
            Channel = subscription.Channel,
            SubscriptionData = subscription.GetSubscriptionData()
        });
        await dbContext.SaveChangesAsync();
    }

    public Task UpsertSubscriptionPreference<T>(INotificationChannelSubscription<T> subscription, bool enabled) where T : INotificationChannelSubscription<T>
    {
        throw new NotImplementedException();
    }

    public async Task RemoveSubscriptionPreference<T>(INotificationChannelSubscription<T> subscription) where T : INotificationChannelSubscription<T>
    {
        var dbSubscription = await dbContext.NotificationSubscriptions.Where(x =>
            x.UserId == subscription.UserId && x.SubscriptionData == subscription.GetSubscriptionData() &&
            x.Channel == subscription.Channel).ToListAsync();

        if (dbSubscription.Count != 0)
        {
            dbContext.NotificationSubscriptions.RemoveRange(dbSubscription);
            await dbContext.SaveChangesAsync();
        }
    }

    public IAsyncEnumerable<TSubscription> SavedSubscriptionPreferences<TNotificationChannel, TSubscription>() where TNotificationChannel : INotificationChannel where TSubscription : INotificationChannelSubscription<TSubscription>
    {
        var channelName = TNotificationChannel.ChannelName;
        return dbContext.NotificationSubscriptions
            .Where(x => x.Channel == channelName)
            .AsAsyncEnumerable()
            .Select(TSubscription.FromDatabase);
    }

    public async Task<AutoSubscriptionPreference> GetAutoSubscriptionPreference<TAutoSubscription, TChannel>(ulong userId, bool defaultValue) where TAutoSubscription : IAutoSubscription<TChannel> where TChannel : INotificationChannel
    {
        var autoSubscriptionEventName = TAutoSubscription.AutoSubscriptionEventName;
        var channelName = TChannel.ChannelName;
        var entry = await dbContext.NotificationEventAutoSubscriptions.FirstOrDefaultAsync(
            x => x.Event == autoSubscriptionEventName && x.UserId == userId && x.Channel == channelName);

        if (entry is null)
        {
            // Insert a new entry
            entry = new NotificationEventAutoSubscription
            {
                Enabled = defaultValue,
                Channel = channelName,
                Event = autoSubscriptionEventName,
                UserId = userId
            };
            dbContext.NotificationEventAutoSubscriptions.Add(entry);
            await dbContext.SaveChangesAsync();
        }

        return new AutoSubscriptionPreference(entry, entry.Enabled);
    }

    public async Task SetAutoSubscriptionPreference<TAutoSubscription, TChannel>(ulong userId, bool isSubscribed) where TAutoSubscription : IAutoSubscription<TChannel> where TChannel : INotificationChannel
    {
        var (subscription, isSubscriptionSubscribed) = await GetAutoSubscriptionPreference<TAutoSubscription, TChannel>(userId, isSubscribed);
        if (isSubscriptionSubscribed != isSubscribed)
        {
            subscription.Enabled = isSubscribed;
            dbContext.Update(subscription);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task SendEmailNotification<TChannel>(ulong userId, Locale locale, object args) where TChannel : INotificationChannel
    {
        var email = new NotificationEmail(locale, TChannel.ChannelName, args);
        var user = await accountsService.UserById(userId);
        await accountsService.SendEmail(user, ("parlance@vicr123.com", "Parlance"), email.Subject, email.Body,
            email.Body);
    }
}
